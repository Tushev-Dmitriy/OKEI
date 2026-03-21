Shader "Custom/GravityBeamURP"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.2, 0.7, 1.0, 0.25)
        _EmissionColor ("Emission Color", Color) = (0.2, 0.7, 1.0, 1.0)
        _EmissionStrength ("Emission Strength", Range(0, 20)) = 5
        _FresnelPower ("Fresnel Power", Range(0.1, 8)) = 3
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2
        _PulseAmount ("Pulse Amount", Range(0, 2)) = 0.35
        _VerticalFade ("Vertical Fade", Range(0, 5)) = 1
        _NoiseScale ("Noise Scale", Range(0.1, 20)) = 4
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.15
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
            Name "ForwardLit"
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
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float3 viewDirWS   : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float _EmissionStrength;
                float _FresnelPower;
                float _PulseSpeed;
                float _PulseAmount;
                float _VerticalFade;
                float _NoiseScale;
                float _NoiseStrength;
            CBUFFER_END

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(a, b, u.x) +
                       (c - a) * u.y * (1.0 - u.x) +
                       (d - b) * u.x * u.y;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normalize(normalInputs.normalWS);
                OUT.uv = IN.uv;
                OUT.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);

                float fresnel = pow(1.0 - saturate(dot(N, V)), _FresnelPower);

                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;

                float vertical = IN.uv.y;
                float verticalMask = pow(saturate(sin(vertical * PI)), _VerticalFade);

                float n = noise(float2(IN.uv.x * _NoiseScale, IN.uv.y * _NoiseScale + _Time.y * 0.75));
                float noiseMask = lerp(1.0, n, _NoiseStrength);

                float alpha = _BaseColor.a * fresnel * verticalMask * pulse * noiseMask;
                alpha = saturate(alpha);

                float3 emission = _EmissionColor.rgb * _EmissionStrength * fresnel * verticalMask * pulse * noiseMask;
                float3 finalColor = _BaseColor.rgb + emission;

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
}