Shader "Custom/UI_GrassSway"
{
    Properties
    {
        [MainTexture] _MainTex("Sprite Texture", 2D) = "white" {}
        _SwaySpeed("Wind Speed", Float) = 2.0        // Скорость качания
        _SwayStrength("Wind Strength", Float) = 15.0 // Насколько сильно гнется
        _BaseY("Base Y Offset (Calibration)", Float) = 0.0 // Калибровка корня
    }

    SubShader
    {
        // Настройки для UI (прозрачность, батчинг)
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
                float _SwaySpeed;
                float _SwayStrength;
                float _BaseY;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // 1. КООРДИНАТЫ (Вычитаем базу, как во флаге, но для Y)
                // В UI Unity часто использует глобальные координаты,
                // поэтому нам нужно найти реальный низ спрайта.
                float yLocal = IN.positionOS.y - _BaseY;

                // 2. ВЕС (Фиксация корня)
                // Мы предполагаем, что высота цветка около 100 пикселей.
                // Гнется только то, что выше 10 пикселей.
                float deadZone = 10.0;
                float height = 100.0;
                
                // saturate обнулит вес, если yLocal меньше deadZone
                float weight = saturate((yLocal - deadZone) / height);
                
                // Делаем изгиб плавным ( pow(2) ), чтобы гнулась в основном верхушка
                weight = pow(weight, 2.0);

                // 3. ФИЗИКА ВЕТРА (Качание маятника)
                // Создаем неравномерный ветер, смешивая два синуса с разной частотой.
                // sin(1.0) - основное качание, sin(1.6) - создает порывы.
                float time = _Time.y * _SwaySpeed;
                float windFactor = sin(time) + sin(time * 1.6) * 0.5;

                // 4. ПРИМЕНЕНИЕ (Смещаем по X, а не по Y)
                // Гнем влево-вправо. У корня weight = 0, поэтому цветок стоит на месте.
                IN.positionOS.x += windFactor * _SwayStrength * weight;

                // 5. ДОПОЛНИТЕЛЬНО (Объем)
                // Когда цветок сильно гнется, он должен немного сжиматься по вертикали.
                // Это добавит веса и объема.
                IN.positionOS.y -= abs(windFactor) * _SwayStrength * 0.1 * weight;


                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target {
                // Умножаем на IN.color, чтобы работал Color в компоненте Image
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
            }
            ENDHLSL
        }
    }
}