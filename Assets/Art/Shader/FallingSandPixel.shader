Shader "FallingSand/PixelRender"
{
    Properties
    {
        _MainTex ("Data Texture", 2D) = "black" {}
        _ColorLUT ("Color LUT", 2D) = "black" {}
        _LUTSize ("LUT Row Count", Float) = 8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "FallingSandPixel"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize; // x=1/width, y=1/height, z=width, w=height

            TEXTURE2D(_ColorLUT);
            SAMPLER(sampler_ColorLUT);

            CBUFFER_START(UnityPerMaterial)
                float _LUTSize;
            CBUFFER_END

            // ---- 哈希函数 ----
            // 简易一维哈希，输入浮点数返回 [0, 1) 伪随机值
            float hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 采样数据纹理 (Point 过滤)
                float4 data = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // 解码像素数据
                // R = PixelType ID, G = seed, B = modulate
                uint pixelType = (uint)(data.r * 255.0 + 0.5);
                float seed = data.g * 255.0;       // 还原为 0~255
                float modulate = data.b;            // 0.0 ~ 1.0

                // Empty (type=1) 或 Disable (type=0) 像素透明
                if (pixelType <= 1)
                {
                    return half4(0, 0, 0, 0);
                }

                // 从 Color LUT 查找颜色和噪声参数
                // LUT 布局: 4 列 x N 行
                // 列0 = baseColor, 列1 = noiseLowIntensity, 列2 = noiseMidIntensity, 列3 = noiseHighIntensity
                float lutV = ((float)pixelType + 0.5) / _LUTSize;
                float4 baseColor = SAMPLE_TEXTURE2D(_ColorLUT, sampler_ColorLUT, float2(0.125, lutV));
                float lowInt  = SAMPLE_TEXTURE2D(_ColorLUT, sampler_ColorLUT, float2(0.375, lutV)).r;
                float midInt  = SAMPLE_TEXTURE2D(_ColorLUT, sampler_ColorLUT, float2(0.625, lutV)).r;
                float highInt = SAMPLE_TEXTURE2D(_ColorLUT, sampler_ColorLUT, float2(0.875, lutV)).r;

                // ---- 三频段种子噪声 ----
                // 每个频段使用不同的哈希偏移，产生独立的伪随机扰动
                // 纯基于 seed，不依赖当前位置，像素移动后颜色保持一致

                // 低频噪声 (结构): 将 seed 量化到较少档位，产生大块明暗分组
                float seedLow = floor(seed / 32.0); // 量化为 8 档
                float noiseLow = (hash11(seedLow * 7.13 + 1.7) - 0.5) * 2.0 * lowInt;
 
                // 中频噪声 (纹理): 将 seed 量化到中等档位
                float seedMid = floor(seed / 8.0);  // 量化为 32 档
                float noiseMid = (hash11(seedMid * 13.37 + 53.7) - 0.5) * 2.0 * midInt;

                // 高频噪声 (脏污): 直接使用 seed，每像素独立
                float noiseHigh = (hash11(seed * 31.17 + 127.1) - 0.5) * 2.0 * highInt;

                // 叠加三频段噪声到基础颜色
                float totalNoise = noiseLow + noiseMid + noiseHigh;
                float4 pixelColor = baseColor;
                pixelColor.rgb += totalNoise;

                // ---- Modulate 叠加 ----
                // modulate 表示外部影响（火烧、侵蚀等）
                // 越高越偏向焦黑色
                float3 modulateColor = float3(0.15, 0.08, 0.02); // 焦黑/烧焦色
                pixelColor.rgb = lerp(pixelColor.rgb, modulateColor, modulate * 0.8);

                pixelColor.rgb = saturate(pixelColor.rgb);
                pixelColor.a = 1.0;

                return half4(pixelColor);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
