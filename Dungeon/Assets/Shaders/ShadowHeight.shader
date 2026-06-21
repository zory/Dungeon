Shader "Hidden/Dungeon/ShadowHeight"
{
    // Renders shadow geometry into the shadow height map.
    // Each pixel receives the caster's normalized height via vertex color.
    // BlendOp Max ensures the tallest shadow wins at each pixel.

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

        BlendOp Max
        Blend One One
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "ShadowHeight"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float  height     : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.height = v.color.r;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(i.height, i.height, i.height, 1);
            }
            ENDHLSL
        }
    }
}
