Shader "Dungeon/DualGridTile"
{
    // Tinted atlas sampler for the dual-grid autotile system.
    //
    // _Invert = 0  →  solid layer: renders pixels where the atlas is opaque
    // _Invert = 1  →  void  layer: renders pixels where the atlas is transparent
    //
    // The atlas carries only shape info (white = solid, transparent = void).
    // Actual color comes from per-vertex color set by DualGridChunkRenderer.
    Properties
    {
        _MainTex ("Tile Atlas", 2D) = "white" {}
        _Invert  ("Invert Alpha (0 = solid, 1 = void)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "UnlitForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float  _Invert;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;        // per-vertex tint from DualGridChunkRenderer
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : TEXCOORD1;    // COLOR interpolator unreliable in URP; use TEXCOORD
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color      = IN.color;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float texAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a;
                float alpha    = lerp(texAlpha, 1.0 - texAlpha, _Invert);
                return float4(IN.color.rgb, IN.color.a * alpha);
            }
            ENDHLSL
        }
    }
}
