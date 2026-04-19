Shader "Hidden/Simulated Camera/Exposure Accumulation"
{
    Properties
    {
        _AccumTex ("Accumulated", 2D) = "black" {}
        _SampleTex ("Sample", 2D) = "black" {}
        _SampleWeight ("Sample Weight", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _AccumTex;
            sampler2D _SampleTex;
            float _SampleWeight;

            fixed4 frag(v2f_img input) : SV_Target
            {
                float2 uv = input.uv;
                float3 accumulated = tex2D(_AccumTex, uv).rgb;
                float3 sampleColor = tex2D(_SampleTex, uv).rgb * _SampleWeight;
                return fixed4(accumulated + sampleColor, 1.0);
            }
            ENDCG
        }
    }
}
