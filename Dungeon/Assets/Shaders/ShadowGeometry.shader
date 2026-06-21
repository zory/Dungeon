Shader "Hidden/Dungeon/ShadowGeometry"
{
    // Solid black shader used by LightMapRendererFeature to render shadow volumes
    // into the light map. Uses BlendOp Min so it can only darken existing light values —
    // shadow geometry writes black (0) which, via Min, overwrites any lit (1) pixel.

    Properties
    {
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        BlendOp Min
        Blend One One
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "ShadowGeometry"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(0, 0, 0, 1);
            }
            ENDHLSL
        }
    }
}
