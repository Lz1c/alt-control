Shader "Custom/AuroraSpirit2DURP"
{
    Properties
    {
        [MainTexture] _MainTex ("Aurora Texture", 2D) = "white" {}
        [MainColor] _Tint ("Tint", Color) = (0.65, 1.0, 0.86, 1.0)
        _SecondaryTint ("Secondary Tint", Color) = (0.45, 0.95, 1.0, 1.0)
        _Alpha ("Alpha", Range(0, 2)) = 1
        _EmissionStrength ("Emission Strength", Range(0, 8)) = 2.5
        _BreathSpeed ("Breath Speed", Range(0, 3)) = 0.75
        _BreathAmount ("Breath Amount", Range(0, 1)) = 0.14
        _FlowSpeed ("Flow Speed", Range(-2, 2)) = 0.08
        _FlowScale ("Flow Scale", Range(0.1, 12)) = 3.5
        _DistortStrength ("Distort Strength", Range(0, 0.1)) = 0.02
        _DistortScale ("Distort Scale", Range(0.1, 12)) = 4
        _PulseSharpness ("Pulse Sharpness", Range(0.5, 6)) = 2.2
        _VerticalFade ("Vertical Fade", Range(0, 2)) = 0.35
        _UseColorMask ("Use Color Mask", Range(0, 1)) = 1
        _MaskBias ("Mask Bias", Range(-1, 1)) = 0.08
        _MaskScale ("Mask Scale", Range(0.1, 8)) = 2.4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 flowUv : TEXCOORD1;
                float2 distortUv : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Tint;
                half4 _SecondaryTint;
                half _Alpha;
                half _EmissionStrength;
                half _BreathSpeed;
                half _BreathAmount;
                half _FlowSpeed;
                half _FlowScale;
                half _DistortStrength;
                half _DistortScale;
                half _PulseSharpness;
                half _VerticalFade;
                half _UseColorMask;
                half _MaskBias;
                half _MaskScale;
            CBUFFER_END

            float2 Hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453123);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = dot(Hash22(i + float2(0.0, 0.0)), float2(1.0, 0.0));
                float b = dot(Hash22(i + float2(1.0, 0.0)), float2(1.0, 0.0));
                float c = dot(Hash22(i + float2(0.0, 1.0)), float2(1.0, 0.0));
                float d = dot(Hash22(i + float2(1.0, 1.0)), float2(1.0, 0.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float FractalNoise(float2 p)
            {
                float n = 0.0;
                float amp = 0.5;
                n += ValueNoise(p) * amp;
                p = p * 2.03 + 17.13;
                amp *= 0.5;
                n += ValueNoise(p) * amp;
                p = p * 2.01 + 31.71;
                amp *= 0.5;
                n += ValueNoise(p) * amp;
                return n / 0.875;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.flowUv = input.uv;
                output.distortUv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y;

                float2 baseUv = input.uv;
                float2 distortionUv = input.distortUv * _DistortScale + float2(time * 0.11, -time * 0.17);
                float2 distortion = (float2(
                    FractalNoise(distortionUv),
                    FractalNoise(distortionUv + 19.37))
                    - 0.5) * 2.0 * _DistortStrength;

                float2 sampledUv = baseUv + distortion;
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampledUv);

                float2 flowUv = input.flowUv * _FlowScale + float2(0.0, time * _FlowSpeed);
                float flowA = FractalNoise(flowUv);
                float flowB = FractalNoise(flowUv * 1.83 + float2(2.13, -time * (_FlowSpeed * 0.6)));
                float flow = saturate(lerp(flowA, flowB, 0.5));

                half luminance = dot(tex.rgb, half3(0.2126h, 0.7152h, 0.0722h));
                half greenDominance = tex.g - max(tex.r, tex.b) * 0.65h;
                half computedMask = saturate((luminance * 0.75h + greenDominance - _MaskBias) * _MaskScale);
                half sourceAlpha = lerp(tex.a, computedMask, _UseColorMask);

                half verticalMask = saturate(lerp(1.0h, smoothstep(0.0h, 1.0h, input.flowUv.y), _VerticalFade));
                half pulse = pow(saturate(0.5h + 0.5h * sin(time * _BreathSpeed * 6.2831853h)), _PulseSharpness);
                half breath = 1.0h + ((pulse * 2.0h - 1.0h) * _BreathAmount);

                half edgeGlow = saturate(flow * 1.25h + pulse * 0.35h);
                half3 tint = lerp(_Tint.rgb, _SecondaryTint.rgb, edgeGlow);

                half alpha = saturate(sourceAlpha * _Alpha * verticalMask * lerp(0.8h, 1.2h, flow));
                half emissionMask = saturate(sourceAlpha * (0.65h + flow * 0.75h) * verticalMask);
                half3 color = tex.rgb * tint * emissionMask * _EmissionStrength * breath;

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
