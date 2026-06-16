Shader "Dungeon/DualGridTile"
{
    // Per-layer dual-grid tile shader.
    //
    // Each terrain type renders as a separate sub-mesh layer, stacked
    // bottom-to-top by priority with alpha blending.
    //
    // Per-vertex COLOR     = terrain tint (RGBA; A < 1 for semi-transparent terrain)
    // Per-vertex TEXCOORD1 = flags: x = invert shape (0 normal, 1 complement)
    //
    // _MainTex (Shape Atlas):
    //   Alpha defines terrain presence (1 = terrain, 0 = see-through).
    //   8 tiles with complement trick: shader inverts alpha when flag is set.
    //   RGB is ignored.
    //
    // _OutlineTex (Outline Atlas, optional):
    //   Alpha defines where border lines are drawn.
    //   RGB defines the outline colour (typically black).
    //   Same 8-tile layout as the shape atlas.  NOT inverted on complement.
    //   Draw border pixels on BOTH sides of each transition edge so that
    //   both the direct and complement bitmask show an outline within
    //   their filled region.
    Properties
    {
        _MainTex    ("Shape Atlas",   2D) = "white" {}
        _OutlineTex ("Outline Atlas", 2D) = "black" {}
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
            sampler2D _OutlineTex;
            float4    _MainTex_ST;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;      // terrain tint (RGBA)
                float4 flags  : TEXCOORD1;  // x = invert shape (0 or 1)
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : TEXCOORD1;
                fixed  invert : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos    = UnityObjectToClipPos(v.vertex);
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                o.color  = v.color;
                o.invert = v.flags.x;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed  shapeA   = tex2D(_MainTex, i.uv).a;
                fixed4 outline  = tex2D(_OutlineTex, i.uv);

                // Invert shape alpha for complement bitmask tiles.
                fixed shape = lerp(shapeA, 1.0 - shapeA, i.invert);

                // Blend terrain tint towards outline colour where outline is present.
                fixed3 rgb = lerp(i.color.rgb, outline.rgb, outline.a);

                return fixed4(rgb, shape * i.color.a);
            }
            ENDCG
        }
    }
}
