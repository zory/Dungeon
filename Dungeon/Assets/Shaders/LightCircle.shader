Shader "Dungeon/LightCircle"
{
    // Simple unlit shader that renders a quad as a hard-edged circle.
    // Used internally by LightMapRendererFeature to stamp each light source
    // into the binary light map texture.
    //
    // Additive blending: overlapping lights accumulate towards white.
    // The R8_UNorm render target naturally clamps the result to [0,1].

    Properties
    {
        // No user-facing properties — the render pass sets everything via MaterialPropertyBlock.
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend One One   // Additive: src + dst
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "LightCircle"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Hard circle: white inside radius 0.5 from centre, black outside.
                float dist = length(i.uv - 0.5);
                half circle = step(dist, 0.5);
                return half4(circle, circle, circle, circle);
            }
            ENDHLSL
        }
    }
}
