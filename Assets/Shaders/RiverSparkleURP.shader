Shader "Custom/RiverSparkle_Final"
{
    Properties
    {
        [MainTexture] _BaseMap("1. Clean River (Texture)", 2D) = "white" {}
        _GlowMask("2. Glow Mask (Original highlights)", 2D) = "black" {}
        
        _SparkleSpeed("3. Speed", Float) = 2.0
        _Density("4. Randomness Density (Set 500-1000)", Float) = 500.0
        _Threshold("5. Appearance Threshold (0 to 1)", Range(0, 1)) = 0.8
        _EmissionPower("6. Brightness Multiplier", Float) = 5.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_GlowMask); SAMPLER(sampler_GlowMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _SparkleSpeed;
                float _Density;
                float _Threshold;
                float _EmissionPower;
            CBUFFER_END

            float hash(float2 p) {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            Varyings vert(Attributes IN) {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target {
                half4 mainColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * IN.color;
                half4 maskTex = SAMPLE_TEXTURE2D(_GlowMask, sampler_GlowMask, IN.uv);

                // Островная логика (чтобы блик не мылило)
                float2 snappedUV = floor(IN.uv * _Density) / _Density; 
                float seed = hash(snappedUV);

                float wave = sin(_Time.y * _SparkleSpeed + seed * 62.83);
                wave = saturate(wave * 0.5 + 0.5);

                // Жесткое включение/выключение
                float visibility = step(_Threshold, pow(wave, 2.0));

                // Применяем оригинальный пиксель из маски
                mainColor.rgb += maskTex.rgb * visibility * _EmissionPower;

                return mainColor;
            }
            ENDHLSL
        }
    }
}