Shader "Custom/URP/SimpleGlass"
{
    Properties
    {
        _BaseColor("Glass Tint", Color) = (0.7, 0.9, 1.0, 0.25)
        _MainTex("Base Texture", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalStrength("Normal Strength", Range(0, 2)) = 1.0

        _ReflectionCubemap("Reflection Cubemap", Cube) = "" {}
        _ReflectionStrength("Reflection Strength", Range(0, 2)) = 0.6

        _FresnelPower("Fresnel Power", Range(0.1, 8)) = 4.0
        _FresnelStrength("Fresnel Strength", Range(0, 2)) = 1.0

        _Alpha("Alpha", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 tangentWS   : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float2 uv          : TEXCOORD4;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            TEXTURECUBE(_ReflectionCubemap);
            SAMPLER(sampler_ReflectionCubemap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MainTex_ST;
                float4 _NormalMap_ST;
                float _NormalStrength;
                float _ReflectionStrength;
                float _FresnelPower;
                float _FresnelStrength;
                float _Alpha;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normalize(normalInputs.normalWS);
                OUT.tangentWS = normalize(normalInputs.tangentWS);
                OUT.bitangentWS = normalize(normalInputs.bitangentWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);

                return OUT;
            }

            float3 GetNormalWS(Varyings IN)
            {
                float4 normalSample = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv);
                float3 normalTS = UnpackNormal(normalSample);
                normalTS.xy *= _NormalStrength;

                float3x3 tbn = float3x3(
                    normalize(IN.tangentWS),
                    normalize(IN.bitangentWS),
                    normalize(IN.normalWS)
                );

                return normalize(mul(normalTS, tbn));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = GetNormalWS(IN);
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionWS));

                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float3 tint = baseTex.rgb * _BaseColor.rgb;

                float3 reflDir = reflect(-viewDirWS, normalWS);
                float3 reflection = SAMPLE_TEXTURECUBE(_ReflectionCubemap, sampler_ReflectionCubemap, reflDir).rgb;

                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower) * _FresnelStrength;

                float3 finalColor = tint + reflection * (_ReflectionStrength * fresnel);
                float finalAlpha = saturate(_Alpha * _BaseColor.a + fresnel * 0.15);

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}