Shader "Custom/AuroraSpiritSpriteURP"
{
    Properties
    {
        [PerRendererData][MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        [MainColor] _Color ("Tint", Color) = (1, 1, 1, 1)
        _AuroraTint ("Aurora Tint", Color) = (0.65, 1.0, 0.86, 1.0)
        _SecondaryTint ("Secondary Tint", Color) = (0.45, 0.95, 1.0, 1.0)
        _EdgeTint ("Edge Tint", Color) = (0.75, 1.0, 0.98, 1.0)
        _Alpha ("Alpha", Range(0, 2)) = 1
        _EmissionStrength ("Emission Strength", Range(0, 8)) = 2.5
        _ExposureResponse ("Exposure Response", Range(0, 2)) = 0.45
        _ExposureAlphaResponse ("Exposure Alpha Response", Range(0, 1)) = 0.03
        _ExposureDarkenResponse ("Exposure Darken Response", Range(0, 1)) = 0.12
        _ExposureMaxBoost ("Exposure Max Boost", Range(1, 4)) = 1.55
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
        _EdgeFadeStart ("Edge Fade Start", Range(0, 1)) = 0.08
        _EdgeFadeEnd ("Edge Fade End", Range(0.01, 1)) = 0.42
        _EdgeNoiseScale ("Edge Noise Scale", Range(0.1, 12)) = 4.5
        _EdgeNoiseSpeed ("Edge Noise Speed", Range(-2, 2)) = 0.12
        _EdgeNoiseStrength ("Edge Noise Strength", Range(0, 0.3)) = 0.08
        _EdgeGradientStrength ("Edge Gradient Strength", Range(0, 2)) = 0.7
        _EdgeGradientWidth ("Edge Gradient Width", Range(0.01, 1)) = 0.28
        _EdgeGradientSoftness ("Edge Gradient Softness", Range(0.01, 1)) = 0.18
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "SpriteAurora"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 flowUv : TEXCOORD1;
                float2 distortUv : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _SimulatedCameraExposureEV;
            float _SimulatedAuroraPhotoAlphaBoost;
            float _SimulatedAuroraPhotoFadeSoftening;
            float _SimulatedAuroraPhotoDefinitionBoost;
            float _SimulatedAuroraPhotoEdgeStability;
            float _SimulatedAuroraPhotoContentBoost;

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _AuroraTint;
                half4 _SecondaryTint;
                half4 _EdgeTint;
                half _Alpha;
                half _EmissionStrength;
                half _ExposureResponse;
                half _ExposureAlphaResponse;
                half _ExposureDarkenResponse;
                half _ExposureMaxBoost;
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
                half _EdgeFadeStart;
                half _EdgeFadeEnd;
                half _EdgeNoiseScale;
                half _EdgeNoiseSpeed;
                half _EdgeNoiseStrength;
                half _EdgeGradientStrength;
                half _EdgeGradientWidth;
                half _EdgeGradientSoftness;
            CBUFFER_END

            #ifdef UNITY_INSTANCING_ENABLED
                UNITY_INSTANCING_BUFFER_START(PerDrawSprite)
                    UNITY_DEFINE_INSTANCED_PROP(half4, _RendererColor)
                UNITY_INSTANCING_BUFFER_END(PerDrawSprite)
            #endif

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

                half4 rendererColor = half4(1, 1, 1, 1);
                #ifdef UNITY_INSTANCING_ENABLED
                    rendererColor = UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, _RendererColor);
                #endif
                output.color = input.color * _Color * rendererColor;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y;

                float2 distortionUv = input.distortUv * _DistortScale + float2(time * 0.11, -time * 0.17);
                float2 distortion = (float2(
                    FractalNoise(distortionUv),
                    FractalNoise(distortionUv + 19.37))
                    - 0.5) * 2.0 * _DistortStrength;

                float2 sampledUv = input.uv + distortion;
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampledUv) * input.color;

                float2 flowUv = input.flowUv * _FlowScale + float2(0.0, time * _FlowSpeed);
                float flowA = FractalNoise(flowUv);
                float flowB = FractalNoise(flowUv * 1.83 + float2(2.13, -time * (_FlowSpeed * 0.6)));
                float flow = saturate(lerp(flowA, flowB, 0.5));

                half luminance = dot(tex.rgb, half3(0.2126h, 0.7152h, 0.0722h));
                half greenDominance = tex.g - max(tex.r, tex.b) * 0.65h;
                half contentMask = saturate(flow * 0.75h + greenDominance * 0.65h + luminance * 0.25h);
                half computedMask = saturate((luminance * 0.75h + greenDominance - _MaskBias) * _MaskScale);
                computedMask = saturate(computedMask + contentMask * _SimulatedAuroraPhotoContentBoost * 0.35h);
                half sourceAlpha = lerp(tex.a, computedMask, _UseColorMask);
                sourceAlpha = saturate(lerp(sourceAlpha, pow(sourceAlpha, 0.8h), _SimulatedAuroraPhotoDefinitionBoost));
                sourceAlpha = saturate(sourceAlpha + _SimulatedAuroraPhotoContentBoost * 0.08h);

                half verticalMask = saturate(lerp(1.0h, smoothstep(0.0h, 1.0h, input.flowUv.y), _VerticalFade));
                half pulse = pow(saturate(0.5h + 0.5h * sin(time * _BreathSpeed * 6.2831853h)), _PulseSharpness);
                half breath = 1.0h + ((pulse * 2.0h - 1.0h) * _BreathAmount);

                float2 edgeNoiseUv = input.flowUv * _EdgeNoiseScale + float2(time * _EdgeNoiseSpeed, -time * (_EdgeNoiseSpeed * 0.73));
                half edgeNoise = (half(FractalNoise(edgeNoiseUv)) - 0.5h) * 2.0h;
                half edgeNoiseStrength = lerp(_EdgeNoiseStrength, _EdgeNoiseStrength * 0.2h, _SimulatedAuroraPhotoEdgeStability);
                half noisyAlpha = saturate(sourceAlpha + edgeNoise * edgeNoiseStrength);
                half edgeFade = smoothstep(_EdgeFadeStart, max(_EdgeFadeStart + 0.001h, _EdgeFadeEnd), noisyAlpha);
                edgeFade = lerp(edgeFade, 1.0h, _SimulatedAuroraPhotoFadeSoftening);
                half edgeGlow = saturate(flow * 1.25h + pulse * 0.35h);
                half edgeBand = 1.0h - smoothstep(_EdgeGradientWidth, _EdgeGradientWidth + _EdgeGradientSoftness, noisyAlpha);
                edgeBand *= smoothstep(0.001h, 0.08h + _EdgeGradientSoftness, noisyAlpha);
                half edgeGradient = saturate(edgeBand * _EdgeGradientStrength);
                half positiveExposure = max(0.0h, (half)_SimulatedCameraExposureEV);
                half negativeExposure = max(0.0h, (half)(-_SimulatedCameraExposureEV));
                half exposureBoost = min(_ExposureMaxBoost, 1.0h + positiveExposure * _ExposureResponse);
                half exposureDarken = 1.0h / (1.0h + negativeExposure * _ExposureDarkenResponse);
                half exposureMultiplier = exposureBoost * exposureDarken;
                half exposureAlphaBoost = lerp(1.0h, exposureMultiplier, _ExposureAlphaResponse);

                half3 tint = lerp(_AuroraTint.rgb, _SecondaryTint.rgb, edgeGlow);
                tint = lerp(tint, _EdgeTint.rgb, edgeGradient);

                half alpha = saturate(sourceAlpha * edgeFade * _Alpha * verticalMask * lerp(0.8h, 1.2h, flow) * exposureAlphaBoost * _SimulatedAuroraPhotoAlphaBoost);
                half emissionMask = saturate(sourceAlpha * edgeFade * (0.65h + flow * 0.75h + edgeGradient * 0.45h) * verticalMask);
                half luminanceWeight = saturate(pow(max(0.0001h, emissionMask), 0.65h));
                half photoExposureResponse = lerp(1.0h, exposureMultiplier, luminanceWeight * (1.0h - _SimulatedAuroraPhotoContentBoost * 0.35h));
                half3 color = tex.rgb * tint * emissionMask * _EmissionStrength * breath * photoExposureResponse;

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
