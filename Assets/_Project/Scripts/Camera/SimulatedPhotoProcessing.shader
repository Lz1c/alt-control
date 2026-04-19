Shader "Hidden/Simulated Camera/Photo Processing"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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

            sampler2D _MainTex;
            float4 _NoiseParams;
            float4 _MotionParams;
            float4 _RotationParams;
            float _NoiseSeed;

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float FineNoise(float2 p, float seed)
            {
                float a = Hash(p + float2(seed, 17.0));
                float b = Hash(p + float2(41.0, seed * 1.7));
                float c = Hash(p + float2(seed * 2.3, 83.0));
                float d = Hash(p + float2(113.0, seed * 3.1));
                return (a + b + c + d - 2.0) * 0.5;
            }

            float SmoothNoise(float2 p, float seed)
            {
                float2 cell = floor(p);
                float2 local = frac(p);
                local = local * local * (3.0 - 2.0 * local);

                float a = Hash(cell + float2(seed, seed * 0.37));
                float b = Hash(cell + float2(1.0 + seed, seed * 0.37));
                float c = Hash(cell + float2(seed, 1.0 + seed * 0.37));
                float d = Hash(cell + float2(1.0 + seed, 1.0 + seed * 0.37));

                return lerp(lerp(a, b, local.x), lerp(c, d, local.x), local.y) * 2.0 - 1.0;
            }

            fixed4 frag(v2f_img input) : SV_Target
            {
                float2 uv = input.uv;
                float2 motion = _MotionParams.xy;
                float motionStrength = saturate(_MotionParams.z);
                float radialStrength = clamp(_MotionParams.w, -1.0, 1.0);
                float radialAmount = abs(radialStrength);
                float rollStrength = clamp(_RotationParams.x, -1.0, 1.0);
                float rollAmount = abs(rollStrength);
                float2 centerDirection = uv - 0.5;
                float2 radialDirection = dot(centerDirection, centerDirection) > 0.000001
                    ? normalize(centerDirection) * sign(radialStrength)
                    : float2(0.0, 0.0);
                float2 tangentialDirection = dot(centerDirection, centerDirection) > 0.000001
                    ? normalize(float2(-centerDirection.y, centerDirection.x)) * sign(rollStrength)
                    : float2(0.0, 0.0);

                float3 color = 0;
                const int sampleCount = 9;
                [unroll]
                for (int i = 0; i < sampleCount; i++)
                {
                    float t = (i / 8.0) - 0.5;
                    float2 directionalOffset = motion * t * motionStrength;
                    float2 radialOffset = radialDirection * t * radialAmount * 0.05;
                    float2 rollOffset = tangentialDirection * t * rollAmount * length(centerDirection) * 0.1;
                    color += tex2D(_MainTex, uv + directionalOffset + radialOffset + rollOffset).rgb;
                }
                color /= sampleCount;

                float luminanceNoiseAmount = _NoiseParams.x;
                float chromaNoiseAmount = _NoiseParams.y;
                float darkAreaResponse = _NoiseParams.z;
                float chromaBlockSize = max(_NoiseParams.w, 1.0);
                float2 pixel = uv * _ScreenParams.xy;

                float luminance = dot(color, float3(0.2126, 0.7152, 0.0722));
                float darkWeight = lerp(1.0, 1.0 - sqrt(saturate(luminance)), darkAreaResponse);
                float sensorWeight = 0.25 + darkWeight * 0.75;

                float fineNoise = FineNoise(pixel, _NoiseSeed);
                float2 chromaUv = pixel / chromaBlockSize;
                float redChroma = SmoothNoise(chromaUv, _NoiseSeed + 31.0);
                float blueChroma = SmoothNoise(chromaUv, _NoiseSeed + 97.0);
                float greenChroma = -(redChroma + blueChroma) * 0.35;

                float lumaShift = fineNoise * luminanceNoiseAmount * sensorWeight;
                float3 chromaShift = float3(redChroma, greenChroma, blueChroma) * chromaNoiseAmount * sensorWeight;
                color += lumaShift.xxx + chromaShift;

                return fixed4(saturate(color), 1.0);
            }
            ENDCG
        }
    }
}
