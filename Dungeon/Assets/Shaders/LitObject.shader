Shader "Dungeon/LitObject"
{
    // Simple sprite shader with binary lighting from the global light map.
    //
    // Used for all non-terrain objects: obstacles, characters, items, etc.
    // Lit areas show the sprite normally. Unlit areas show inverted RGB
    // (white outlines on black) with no colour tint — only shape is preserved.
    //
    // When the LightMap renderer feature is not active, Unity provides a
    // white default texture → step(0.5, 1.0) = 1 → fully lit fallback.

    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        [MainColor]   _Color   ("Tint Color", Color) = (1, 1, 1, 1)
        _ObjectHeight ("Object Height", Float) = 0
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
            Name "LitObject"
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
            // Shadow height map: R = normalized max caster height at this pixel.
            TEXTURE2D(_ShadowHeightMap);
            float4 _LightMapParams; // xy = screen width/height

            // Maximum height used for normalization (must match LightMapRendererFeature.MAX_SHADOW_HEIGHT).
            static const float MAX_SHADOW_HEIGHT = 10.0;

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                float  _ObjectHeight; // Height of this object for shadow receiving.
            CBUFFER_END

            // ── Vertex structures ────────────────────────────────────────────

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
            };

            // ── Vertex shader ────────────────────────────────────────────────

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv         = TRANSFORM_TEX(v.uv, _MainTex);
                o.color      = v.color;
                return o;
            }

            // ── Fragment shader ──────────────────────────────────────────────

            half4 frag(Varyings i) : SV_Target
            {
                // Sample the sprite texture.
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_point_clamp, i.uv);

                // Combine texture with material tint and vertex colour (Unity sprites use vertex colour for tinting/alpha).
                half4 col = tex * _Color * i.color;

                // Discard fully transparent pixels — no shadow/outline artifacts on invisible areas.
                if (col.a < 0.01h)
                {
                    discard;
                }

                // Binary lighting: sample the global light map and shadow height map.
                float2 screenUV = i.positionCS.xy / _LightMapParams.xy;
                half baseLight = step(0.5h, SAMPLE_TEXTURE2D(_LightMap, sampler_point_clamp, screenUV).r);
                half shadowHeight = SAMPLE_TEXTURE2D(_ShadowHeightMap, sampler_point_clamp, screenUV).r;

                // Object is in shadow if a taller caster's shadow covers it.
                // shadowHeight is normalized (0-1), compare against this object's normalized height.
                half normalizedHeight = _ObjectHeight / MAX_SHADOW_HEIGHT;
                half inShadow = step(normalizedHeight + 0.001h, shadowHeight);
                half lit = baseLight * (1.0h - inShadow);

                // Lit: normal tinted sprite.
                // Unlit: only outlines (dark pixels) shown as white; everything else pitch black.
                half lum = dot(col.rgb, half3(0.299h, 0.587h, 0.114h));
                half isOutline = 1.0h - step(0.1h, lum);
                half3 unlitRGB = half3(isOutline, isOutline, isOutline);
                col.rgb = lerp(unlitRGB, col.rgb, lit);

                return col;
            }
            ENDHLSL
        }
    }
}
