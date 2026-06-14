Shader "Dungeon/DualGridTile"
{
    // Single-pass dual-grid tile shader.
    //
    // Per-vertex COLOR     = primary tint  (atlas-opaque areas; black pixels stay black as outline)
    // Per-vertex TEXCOORD1 = secondary tint (atlas-transparent areas)
    //
    // Fragment blends the two tints using the atlas alpha:
    //   tex.a == 1  →  primary (luma-tinted so white→tint, black→outline)
    //   tex.a == 0  →  secondary
    //   in between  →  lerp
    Properties
    {
        _MainTex ("Tile Atlas", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;

            struct appdata
            {
                float4 vertex    : POSITION;
                float2 uv        : TEXCOORD0;
                fixed4 color     : COLOR;      // primary tint
                float4 secondary : TEXCOORD1;  // secondary tint (mesh UV1)
            };

            struct v2f
            {
                float4 pos       : SV_POSITION;
                float2 uv        : TEXCOORD0;
                fixed4 primary   : TEXCOORD1;
                fixed4 secondary : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos       = UnityObjectToClipPos(v.vertex);
                o.uv        = TRANSFORM_TEX(v.uv, _MainTex);
                o.primary   = v.color;
                o.secondary = v.secondary;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);

                // Luma-based tint: white atlas pixels → primary colour,
                // black pixels → black outline.
                fixed  luma       = dot(tex.rgb, fixed3(0.299, 0.587, 0.114));
                fixed3 primaryRGB = lerp(fixed3(0, 0, 0), i.primary.rgb, luma);

                // Blend primary (atlas-opaque) and secondary (atlas-transparent).
                fixed3 rgb   = lerp(i.secondary.rgb, primaryRGB, tex.a);

                // Alpha: whichever layer is more visible at this texel.
                // After the C# fixes both colours are always opaque for normal
                // tiles, so this evaluates to 1.  Keeps working if a colour
                // intentionally carries partial alpha (e.g. semi-transparent water).
                fixed  alpha = max(tex.a * i.primary.a, (1.0 - tex.a) * i.secondary.a);

                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
