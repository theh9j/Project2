Shader "Custom/URP Shadow Catcher"
{
    Properties
    {
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.45)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Name "ShadowCatcher"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 shadowCoord : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _ShadowColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.shadowCoord = TransformWorldToShadowCoord(positionWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                Light mainLight = GetMainLight(input.shadowCoord);

                // 1 = fully lit, 0 = fully shadowed
                half shadow = 1.0h - mainLight.shadowAttenuation;

                return half4(_ShadowColor.rgb, shadow * _ShadowColor.a);
            }

            ENDHLSL
        }
    }
}