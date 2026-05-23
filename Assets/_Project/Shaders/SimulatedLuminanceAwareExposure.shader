Shader "Hidden/Simulated Camera/Luminance Aware Exposure"
{
    HLSLINCLUDE

    #include "../../SC Post Effects/Shaders/Pipeline/Pipeline.hlsl"

    float _SimulatedLuminanceExposureEV;
    float _SimulatedLuminanceExposureThreshold;
    float _SimulatedLuminanceExposureSoftness;

    float ComputeLuminanceWeight(float3 color)
    {
        float luminance = dot(color, float3(0.2126, 0.7152, 0.0722));
        return smoothstep(
            _SimulatedLuminanceExposureThreshold,
            _SimulatedLuminanceExposureThreshold + max(0.0001, _SimulatedLuminanceExposureSoftness),
            luminance);
    }

    float4 Frag(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float4 screenColor = ScreenColor(SCREEN_COORDS);
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
