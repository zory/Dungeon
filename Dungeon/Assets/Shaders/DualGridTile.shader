Shader "Dungeon/DualGridTile"
{
    // Single-pass dual-grid terrain shader for URP.
    //
    // Each quad carries up to 4 terrain layers packed into vertex data.
    // The fragment shader composites layers bottom-to-top: samples the tile
    // from the atlas, multiplies RGB by the per-type colour (so white pixels
    // become the terrain colour, dark pixels stay dark for outlines), then
    // composites via source-over alpha blending.
    //
    // VERTEX DATA
    // ───────────
    //   TEXCOORD0 — local quad UV [0,1]
    //   TEXCOORD1 — packed (terrainTileIndex × 4 + rotation) per layer (xyzw)
    //               rotation is always 0 (each bitmask has its own pre-painted tile)
    //   TEXCOORD2 — x = active layer count
    //   TEXCOORD3 — packed RGB colour per layer (xyzw)
    //              (R×65536 + G×256 + B, each channel 0–255)
    //
    // ATLAS TEXTURE (_MainTex)
    // ────────────────────────
    //   Full RGBA tiles.  Alpha = terrain presence (1 inside, 0 outside).
    //   RGB = painted appearance (outlines/borders baked in, typically white
    //         fill with dark border pixels — the per-type colour tints it).
    //   16 tiles per terrain type — one per bitmask (0–15), asymmetric,
    //   no UV rotation applied.

    Properties
    {
        [MainTexture] _MainTex ("Terrain Atlas", 2D) = "white" {}
        _AtlasColumns ("Atlas Columns", Float) = 8
        _AtlasRows    ("Atlas Rows",    Float) = 8
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "DualGridTile"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_point_clamp);

            // Global light map texture set by the LightMap renderer feature.
            // When not set, Unity provides a white default → fully lit fallback.
            TEXTURE2D(_LightMap);
            float4 _LightMapParams; // xy = screen width/height

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_TexelSize;   // auto-set by Unity: (1/w, 1/h, w, h)
                float  _AtlasColumns;
                float  _AtlasRows;
            CBUFFER_END

            // ── Vertex structures ────────────────────────────────────────────

            struct Attributes
            {
                float4 positionOS  : POSITION;
                float2 uv          : TEXCOORD0;
                float4 terrainInfo : TEXCOORD1;  // packed per-layer tile info
                float2 layerInfo   : TEXCOORD2;  // x = layer count
                float4 colorInfo   : TEXCOORD3;  // packed RGB per layer
            };

            struct Varyings
            {
                float4 positionCS                   : SV_POSITION;
                float2 uv                           : TEXCOORD0;
                nointerpolation float4 terrainInfo  : TEXCOORD1;
                nointerpolation float  layerCount   : TEXCOORD2;
                nointerpolation float4 colorInfo    : TEXCOORD3;
            };

            // ── UV rotation ──────────────────────────────────────────────────

            // Rotates a [0,1] quad UV clockwise by rotation × 90°.
            //   rot 0: identity
            //   rot 1 (90° CW):  u' = 1−v, v' = u
            //   rot 2 (180°):    u' = 1−u, v' = 1−v
            //   rot 3 (270° CW): u' = v,   v' = 1−u
            float2 RotateUV(float2 uv, int rot)
            {
                if (rot == 1) { return float2(1.0 - uv.y, uv.x);       }
                if (rot == 2) { return float2(1.0 - uv.x, 1.0 - uv.y); }
                if (rot == 3) { return float2(uv.y, 1.0 - uv.x);       }
                return uv;
            }

            // ── Colour unpacking ─────────────────────────────────────────────

            // Unpacks RGB from a single float: R×65536 + G×256 + B → half3.
            half3 UnpackRGB(float packed)
            {
                float r = floor(packed / 65536.0);
                float rem = packed - r * 65536.0;
                float g = floor(rem / 256.0);
                float b = rem - g * 256.0;
                return half3(r, g, b) / 255.0h;
            }

            // ── Tile sampling ────────────────────────────────────────────────

            // Samples a full RGBA tile from the atlas at the given slot with UV rotation.
            half4 SampleTile(float2 localUV, int tileIdx, int rotation)
            {
                float2 rotated = RotateUV(localUV, rotation);

                int   cols = (int)_AtlasColumns;
                float tileW = 1.0 / _AtlasColumns;
                float tileH = 1.0 / _AtlasRows;
                int   col = tileIdx % cols;
                int   row = tileIdx / cols;
                float2 tileOrigin = float2((float)col * tileW, 1.0 - (float)(row + 1) * tileH);

                // Half-texel inset prevents sampling into adjacent atlas tiles.
                float2 halfTexel = 0.5 * _MainTex_TexelSize.xy;
                float2 tileMin = tileOrigin + halfTexel;
                float2 tileMax = tileOrigin + float2(tileW, tileH) - halfTexel;
                float2 atlasUV = clamp(tileOrigin + rotated * float2(tileW, tileH), tileMin, tileMax);

                return SAMPLE_TEXTURE2D(_MainTex, sampler_point_clamp, atlasUV);
            }

            // ── Vertex shader ────────────────────────────────────────────────

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS  = TransformObjectToHClip(v.positionOS.xyz);
                o.uv          = v.uv;
                o.terrainInfo = v.terrainInfo;
                o.layerCount  = v.layerInfo.x;
                o.colorInfo   = v.colorInfo;
                return o;
            }

            // ── Fragment shader ──────────────────────────────────────────────

            half4 frag(Varyings i) : SV_Target
            {
                int layerCount = (int)round(i.layerCount);

                float packed[4];
                packed[0] = i.terrainInfo.x;
                packed[1] = i.terrainInfo.y;
                packed[2] = i.terrainInfo.z;
                packed[3] = i.terrainInfo.w;

                float colorPacked[4];
                colorPacked[0] = i.colorInfo.x;
                colorPacked[1] = i.colorInfo.y;
                colorPacked[2] = i.colorInfo.z;
                colorPacked[3] = i.colorInfo.w;

                half4 result = half4(0, 0, 0, 0);

                [unroll(4)]
                for (int layer = 0; layer < 4; layer++)
                {
                    if (layer >= layerCount) { break; }

                    int p       = (int)round(packed[layer]);
                    int tileIdx = p / 4;
                    int rot     = p % 4;

                    half4 tile = SampleTile(i.uv, tileIdx, rot);

                    // Tint tile RGB by the terrain type's colour.
                    // White tile pixels → terrain colour, dark pixels → stay dark (outlines).
                    half3 tintedRGB = tile.rgb * UnpackRGB(colorPacked[layer]);

                    // Source-over composite using tile alpha.
                    result.rgb = lerp(result.rgb, tintedRGB, tile.a);
                    result.a   = saturate(result.a + tile.a * (1.0h - result.a));
                }

                // Binary lighting: sample the global light map using screen UV.
                float2 screenUV = i.positionCS.xy / _LightMapParams.xy;
                half lit = step(0.5h, SAMPLE_TEXTURE2D(_LightMap, sampler_point_clamp, screenUV).r);

                // Lit: normal tinted appearance.
                // Unlit: only outlines (dark pixels) shown as white; everything else pitch black.
                half lum = dot(result.rgb, half3(0.299h, 0.587h, 0.114h));
                half isOutline = 1.0h - step(0.1h, lum);
                half3 unlitRGB = half3(isOutline, isOutline, isOutline);
                result.rgb = lerp(unlitRGB, result.rgb, lit);

                return result;
            }
            ENDHLSL
        }
    }
}
