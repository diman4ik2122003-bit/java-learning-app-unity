Shader "Custom/UI_FlagWind_Universal"
{
    Properties
    {
        [MainTexture] _MainTex("Sprite Texture", 2D) = "white" {}
        _WaveSpeed("Wave Speed", Float) = 5.0
        _WaveStrength("Wave Strength", Float) = 20.0
        _WaveDensity("Wave Density", Float) = 3.0
        
        // Добавляем ползунок для калибровки каждого флага
        _BaseX("Base X Offset", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "CanUseSpriteAtlas"="True" }
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

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _WaveSpeed;
                float _WaveStrength;
                float _WaveDensity;
                float _BaseX; // Добавлено в буфер
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // ВЫЧИТАЕМ смещение из входящей координаты x.
                // Теперь для каждого материала "ноль" будет там, где ты укажешь.
                float xPos = IN.positionOS.x - _BaseX;

                // ТВОЯ АНИМАЦИЯ (БЕЗ ИЗМЕНЕНИЙ)
                float leftFix = 5.0; 
                float weight = saturate((xPos - leftFix) / 100.0);
                weight = pow(weight, 3.0);

                float wave = sin((xPos - leftFix) * _WaveDensity * 0.02 - _Time.y * _WaveSpeed);

                IN.positionOS.y += wave * _WaveStrength * weight;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
            }
            ENDHLSL
        }
    }
}