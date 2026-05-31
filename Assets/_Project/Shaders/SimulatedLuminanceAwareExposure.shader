Shader "Hidden/Simulated Camera/Luminance Aware Exposure"
{
    HLSLINCLUDE

    #include "../../SC Post Effects/Shaders/Pipeline/Pipeline.hlsl"

    float _SimulatedLuminanceExposureEV;
    float _SimulatedLuminanceExposureThreshold;
    float _SimulatedLuminanceExposureSoftness;
    float _SimulatedFocusDistance;
    float _SimulatedFocusClearRange;
    float _SimulatedFocusNearDistance;
    float _SimulatedFocusFarDistance;
    float _SimulatedFocusBlurStrength;
    float _SimulatedFocusBlurRadius;
    float _SimulatedFocusBokehHighlightBoost;

    float ComputeLuminanceWeight(float3 color)
    {
        float luminance = dot(color, float3(0.2126, 0.7152, 0.0722));
        return smoothstep(
            _SimulatedLuminanceExposureThreshold,
            _SimulatedLuminanceExposureThreshold + max(0.0001, _SimulatedLuminanceExposureSoftness),
            luminance);
    }

    void AccumulateBokehSample(float2 uv, float2 offset, float ringWeight, inout float4 accum, inout float weightSum)
    {
        float4 sampleColor = ScreenColor(uv + offset);
        float sampleLuminance = dot(sampleColor.rgb, float3(0.2126, 0.7152, 0.0722));
        float highlightWeight = 1.0 + smoothstep(0.45, 1.5, sampleLuminance) * _SimulatedFocusBokehHighlightBoost;
        float weight = ringWeight * highlightWeight;
        accum += sampleColor * weight;
        weightSum += weight;
    }

    float4 SampleCircularApertureBokeh(float2 uv, float2 radius)
    {
        float4 center = ScreenColor(uv);
        float4 accum = center * 0.35;
        float weightSum = 0.35;

        float2 inner = radius * 0.45;
        float2 middle = radius * 0.72;
        float2 outer = radius;

        AccumulateBokehSample(uv, inner * float2( 1.0000,  0.0000), 0.75, accum, weightSum);
        AccumulateBokehSample(uv, inner * float2( 0.0000,  1.0000), 0.75, accum, weightSum);
        AccumulateBokehSample(uv, inner * float2(-1.0000,  0.0000), 0.75, accum, weightSum);
        AccumulateBokehSample(uv, inner * float2( 0.0000, -1.0000), 0.75, accum, weightSum);

        AccumulateBokehSample(uv, middle * float2( 0.9239,  0.3827), 1.0, accum, weightSum);
        AccumulateBokehSample(uv, middle * float2( 0.3827,  0.9239), 1.0, accum, weightSum);
        AccumulateBokehSample(uv, middle * float2(-0.3827,  0.9239), 1.0, accum, weightSum);
        AccumulateBokehSample(uv, middle * float2(-0.9239,  0.3827), 1.0, accum, weightSum);
        AccumulateBokehSample(uv, middle * float2(-0.9239, -0.3827), 1.0, accum, weightSum);
        AccumulateBokehSample(uv, middle * float2(-0.3827, -0.9239), 1.0, accum, weightSum);
        AccumulateBokehSample(uv, middle * float2( 0.3827, -0.9239), 1.0, accum, weightSum);
        AccumulateBokehSample(uv, middle * float2( 0.9239, -0.3827), 1.0, accum, weightSum);

        AccumulateBokehSample(uv, outer * float2( 1.0000,  0.0000), 1.15, accum, weightSum);
        AccumulateBokehSample(uv, outer * float2( 0.7071,  0.7071), 1.15, accum, weightSum);
        AccumulateBokehSample(uv, outer * float2( 0.0000,  1.0000), 1.15, accum, weightSum);
        AccumulateBokehSample(uv, outer * float2(-0.7071,  0.7071), 1.15, accum, weightSum);
        AccumulateBokehSample(uv, outer * float2(-1.0000,  0.0000), 1.15, accum, weightSum);
        AccumulateBokehSample(uv, outer * float2(-0.7071, -0.7071), 1.15, accum, weightSum);
        AccumulateBokehSample(uv, outer * float2( 0.0000, -1.0000), 1.15, accum, weightSum);
        AccumulateBokehSample(uv, outer * float2( 0.7071, -0.7071), 1.15, accum, weightSum);

        float4 bokeh = accum / weightSum;
        float highlightMask = smoothstep(0.55, 1.6, dot(bokeh.rgb, float3(0.2126, 0.7152, 0.0722)));
        bokeh.rgb += bokeh.rgb * highlightMask * 0.2 * _SimulatedFocusBokehHighlightBoost;
        return bokeh;
    }

    float4 Frag(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float2 uv = SCREEN_COORDS;
        float rawDepth = SAMPLE_DEPTH(uv);
        float eyeDepth = LINEAR_EYE_DEPTH(rawDepth);
        float nearDistance = min(_SimulatedFocusNearDistance, _SimulatedFocusFarDistance);
        float farDistance = max(_SimulatedFocusNearDistance, _SimulatedFocusFarDistance);
        float foregroundFalloff = max(0.05, _SimulatedFocusDistance - nearDistance);
        float backgroundFalloff = max(0.05, farDistance - _SimulatedFocusDistance);
        float foregroundDelta = max(0.0, nearDistance - eyeDepth);
        float backgroundDelta = max(0.0, eyeDepth - farDistance);
        float foregroundBlur = smoothstep(0.0, foregroundFalloff, foregroundDelta) * 1.35;
        float backgroundBlur = smoothstep(0.0, backgroundFalloff, backgroundDelta);
        float rawBlur = max(foregroundBlur, backgroundBlur) * _SimulatedFocusBlurStrength;
        float blurWeight = saturate(rawBlur);
        float blurRadius = min(rawBlur, 4.0);

        float4 sharpColor = ScreenColor(uv);
        float2 bokehRadius = _MainTex_TexelSize.xy * _SimulatedFocusBlurRadius * blurRadius;
        float4 blurredColor = SampleCircularApertureBokeh(uv, bokehRadius);

        float4 screenColor = lerp(sharpColor, blurredColor, blurWeight);
        float weight = ComputeLuminanceWeight(screenColor.rgb);
        float exposureMultiplier = exp2(_SimulatedLuminanceExposureEV);
        float weightedMultiplier = 1.0 + (exposureMultiplier - 1.0) * weight;
        float3 result = screenColor.rgb * weightedMultiplier;

        return float4(result, screenColor.a);
    }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "LuminanceAwareExposure"
            HLSLPROGRAM
            #pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
            #pragma exclude_renderers gles

            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
