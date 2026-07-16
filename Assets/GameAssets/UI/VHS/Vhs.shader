// VHS（ビデオテープ風）ポストエフェクトシェーダー。
// フルスクリーン Blit で 1 パス適用する。各要素の強さはマテリアルで調整し、
// マスター強度 _Intensity で全体を一括スケールする（後日スクリプトから制御する入口）。

Shader "Custom/Vhs"
{
    Properties
    {
        // マスター強度。0 でほぼ素の映像、1 で最大。ここをスクリプトで動かす想定。
        _Intensity ("Master Intensity", Range(0, 1)) = 1.0

        [Header(Chromatic Aberration)]
        // RGB を横方向に分離する量。VHS らしさの主役。
        _ChromaticOffset ("Chromatic Offset", Range(0, 0.02)) = 0.004

        [Header(Scanline)]
        _ScanlineCount ("Scanline Count", Float) = 480
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.25

        [Header(Wave Jitter)]
        // 走査線ごとの横揺れ（ヨレ）の最大幅と速さ。
        _WaveAmplitude ("Wave Amplitude", Range(0, 0.02)) = 0.003
        _WaveSpeed ("Wave Speed", Float) = 5.0

        [Header(Grain Noise)]
        _NoiseIntensity ("Noise Intensity", Range(0, 1)) = 0.15

        [Header(Tracking Band)]
        // 画面を縦に流れる乱れの帯。高さ・速さ・帯内での乱れ強度。
        _TrackingBandHeight ("Tracking Band Height", Range(0, 0.5)) = 0.1
        _TrackingBandSpeed ("Tracking Band Speed", Float) = 0.2
        _TrackingBandIntensity ("Tracking Band Intensity", Range(0, 1)) = 0.6

        [Header(Color)]
        // 彩度（1 で素、小さいほど色あせ）と周辺減光の強さ。
        _Saturation ("Saturation", Range(0, 1)) = 0.75
        _VignettePower ("Vignette Power", Range(0, 2)) = 0.4

        [Header(Signal Loss)]
        // 信号消失（ホワイトノイズ）への移行量。0 で通常の VHS、1 で映像が完全に砂嵐になる。
        // シーン別プリセットではなく、ゲームオーバー演出がスクリプトから 0→1 へ動かす実行時の値。
        _SignalLoss ("Signal Loss", Range(0, 1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "VhsPass"
            ZWrite Off ZTest Always Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Intensity;
            float _ChromaticOffset;
            float _ScanlineCount;
            float _ScanlineIntensity;
            float _WaveAmplitude;
            float _WaveSpeed;
            float _NoiseIntensity;
            float _TrackingBandHeight;
            float _TrackingBandSpeed;
            float _TrackingBandIntensity;
            float _Saturation;
            float _VignettePower;
            float _SignalLoss;

            // 帯の中でグレインと横揺れをどれだけ増幅するか（帯を目立たせるための内部係数）。
            static const float kBandNoiseBoost  = 3.0;
            static const float kBandJitterScale = 0.03;
            // グレインノイズの時間スクロール速度（大きいほどザラつきが速く流れる）。
            static const float kNoiseScrollSpeed = 60.0;
            // 信号消失中の横揺れ。マスター強度 _Intensity とは独立に効かせることで、
            // VHS を控えめに設定したシーンでも「絵が崩れてから砂嵐に呑まれる」流れを成立させる。
            static const float kLossJitterAmplitude  = 0.02;
            static const float kLossJitterSpeedScale = 3.0;
            // ホワイトノイズの横方向の引き伸ばし（1 未満で粒が横に尾を引き、アナログの砂嵐らしくなる）
            // と、その時間スクロール速度。速度は「1 秒間に種が一巡する回数」で、フレーム間で
            // 種が十分にばらける値にしている。
            static const float kStaticStretch     = 0.35;
            static const float kStaticScrollSpeed = 90.0;
            // 0..1 に畳んだ時間を種として散らすための倍率（大きすぎると精度が落ちるため控えめに取る）。
            static const float kStaticSeedScale   = 1000.0;
            // 輝度計算の係数（Rec.601）。彩度低下で色あせさせる基準に使う。
            static const float3 kLumaWeights = float3(0.299, 0.587, 0.114);

            // 疑似乱数。UV と時間から 0..1 のばらつきを作る（グレインノイズ用）。
            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // 走査線 1 本ぶんの横揺れ量。低周波のうねりに高周波の細かい震えを重ね、
            // 単調な正弦波にならないよう VHS のヨレを表現する。
            float LineJitter(float row, float time)
            {
                float slow = sin(row * 8.0 + time * _WaveSpeed);
                float fast = sin(row * 120.0 + time * _WaveSpeed * 3.0);
                return slow + fast * 0.3;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float time = _Time.y;

                // トラッキングノイズ帯：画面を縦に流れる帯の位置と、その内側で 1 に近づくマスク。
                float band = frac(uv.y + time * _TrackingBandSpeed);
                float bandMask = smoothstep(1.0 - _TrackingBandHeight, 1.0, band);

                // 横揺れ（ヨレ）：走査線ごとに UV.x をずらす。帯の中ではさらに大きくずらす。
                float jitter = LineJitter(uv.y, time) * _WaveAmplitude;
                jitter += bandMask * _TrackingBandIntensity * kBandJitterScale;

                // 信号消失が進むほど、より速く大きいヨレを重ねて絵を崩す。
                // 砂嵐への単純なクロスフェードでは「テープが切り替わった」ようにしか見えないため、
                // 先に映像を破綻させてからノイズへ渡すことで信号が失われていく過程として見せる。
                float lossJitter = LineJitter(uv.y, time * kLossJitterSpeedScale) * kLossJitterAmplitude * _SignalLoss;
                uv.x += jitter * _Intensity + lossJitter;

                // 色ずれ：R と B を左右に分離してサンプリングする。
                float chroma = _ChromaticOffset * _Intensity;
                half r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(chroma, 0)).r;
                half g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).g;
                half b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(chroma, 0)).b;
                half3 color = half3(r, g, b);

                // 信号消失：映像をホワイトノイズへ置き換える。ここで混ぜることで、以降の走査線・彩度・
                // ビネットがノイズにも掛かり、「映像だけが消えて同じブラウン管に砂嵐が残る」見え方になる。
                //
                // time はシーン開始からの経過秒で青天井に増えるため、そのまま種に加えると数分後には
                // float の精度が落ちて砂嵐が止まって見える。frac で 0..1 に畳んでから使い、
                // 経過時間によらず粒が流れ続けるようにする。
                float2 staticUv = float2(uv.x * kStaticStretch, uv.y) * _ScreenParams.xy;
                float staticSeed = frac(time * kStaticScrollSpeed) * kStaticSeedScale;
                float staticNoise = Hash(staticUv + staticSeed);
                color = lerp(color, staticNoise.xxx, _SignalLoss);

                // グレインノイズ：ピクセル単位のザラつき。帯の中では増幅する。
                float grain = Hash(uv * _ScreenParams.xy + time * kNoiseScrollSpeed);
                float noiseAmount = _NoiseIntensity * (1.0 + bandMask * kBandNoiseBoost) * _Intensity;
                color += (grain - 0.5) * noiseAmount;

                // 走査線：横縞で周期的に暗くする。
                float scan = sin(uv.y * _ScanlineCount * PI) * 0.5 + 0.5;
                float scanline = 1.0 - scan * _ScanlineIntensity * _Intensity;
                color *= scanline;

                // 彩度低下：輝度へ寄せて色あせさせる。_Intensity=0 なら素の彩度のまま。
                float luma = dot(color, kLumaWeights);
                float sat = lerp(1.0, _Saturation, _Intensity);
                color = lerp(luma.xxx, color, sat);

                // ビネット：中心からの距離で周辺を落とす。
                float2 centered = input.texcoord * 2.0 - 1.0;
                float vignette = 1.0 - dot(centered, centered) * _VignettePower * _Intensity;
                color *= saturate(vignette);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
