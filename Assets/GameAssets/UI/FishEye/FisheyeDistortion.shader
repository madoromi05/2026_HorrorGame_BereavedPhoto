//魚眼レンズシェーダー

Shader "Custom/FisheyeDistortion"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        // 魚眼の強さを変えれる
        _BarrelPower ("Barrel Power", Range(0, 5)) = 1.0
        _VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "FisheyeDistortionPass"
            ZWrite Off ZTest Always Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag   // フラグメントシェーダー
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _BarrelPower;
            float _VignetteStrength;

            // どれくらいゆがませるか
            // 画面のピクセル数ぶん呼ばれる
            float2 BarrelDistort(float2 uv, float power)
            {
                float2 centered = uv * 2.0 - 1.0;      // 画面の中心が原点(0,0)になるようにする
                float dist = length(centered);         // 中心からそのピクセルの距離
                float2 distorted = centered * (1.0 + power * dist * dist);  // ベクトルをスカラーにする処理
                return distorted * 0.5 + 0.5;           // (テクスチャ座標に戻す
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = BarrelDistort(input.texcoord, _BarrelPower);

                // 歪みで画面外にはみ出た部分はTVの縁として黒に
                if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
                    return half4(0, 0, 0, 1);

                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                // ブラウン管の丸みを強調するビネット
                float2 centered = input.texcoord * 2.0 - 1.0;
                float vignette = 1.0 - dot(centered, centered) * _VignetteStrength;
                color.rgb *= saturate(vignette);

                return color;
            }
            ENDHLSL
        }
    }
}