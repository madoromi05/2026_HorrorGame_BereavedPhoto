using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 解析率に応じて Post Processing の Vignette 強度を制御する。
/// AnalyzerUI とは責務が異なる（カメラ効果）ため独立したクラスとして管理する。
/// </summary>
public class AnalyzerVignetteController : MonoBehaviour
{
    [SerializeField] private Volume _volume;
    [SerializeField, Range(0f, 1f)] private float _permanentVignetteIntensity = 0.25f;  // 常時表示するビネット強度
    [SerializeField] private float _maxVignetteIntensity = 0.8f;                        // 解析率100%のときの Vignette 追加強度
    [SerializeField] private float _resetFadeSpeed = 2f;                                // リセット時に元に戻る速さ
    [SerializeField] private float _hidingPulseFrequency = 0.8f;                        // 隠れ中のパルス周波数（Hz）
    [SerializeField, Range(0f, 0.2f)] private float _hidingPulseAmplitude = 0.06f;      // 隠れ中のパルス振幅

    private Vignette _vignette;
    private float _baseIntensity;
    private float _analyzeAdd;
    private float _hidingCurrent;
    private float _hidingTarget;
    private float _hidingFadeSpeed;
    private bool _isHiding;
    private bool _isResetting;
    private float _detectionAdd;

    private void Awake()
    {
        if (_volume == null)
        {
            DebugCustom.LogError("[AnalyzerVignetteController] _volume が未設定です。", this);
            return;
        }
        if (_volume.sharedProfile == null)
        {
            DebugCustom.LogError("[AnalyzerVignetteController] sharedProfile が null です。Volume に Profile を設定してください。", this);
            return;
        }

        _volume.profile = Instantiate(_volume.sharedProfile);

        if (!_volume.profile.TryGet(out _vignette))
            _vignette = _volume.profile.Add<Vignette>(overrides: true);

        _vignette.active = true;
        _vignette.intensity.overrideState = true;
        _vignette.intensity.value = _permanentVignetteIntensity;
        _baseIntensity = _permanentVignetteIntensity;
    }

    private void Update()
    {
        if (_vignette == null) return;

        // 隠れ Vignette をフェード
        _hidingCurrent = Mathf.MoveTowards(_hidingCurrent, _hidingTarget, _hidingFadeSpeed * Time.deltaTime);

        // 解析リセット時に analyzeAdd をフェードアウト
        if (_isResetting)
        {
            _analyzeAdd = Mathf.MoveTowards(_analyzeAdd, 0f, _resetFadeSpeed * Time.deltaTime);
            if (Mathf.Approximately(_analyzeAdd, 0f))
                _isResetting = false;
        }

        ApplyVignette();
    }

    /// <summary>
    /// 解析率（0〜100）を受け取り Vignette 強度に反映する。
    /// EnemyAnalyzer の Update から毎フレーム呼ばれることを想定。
    /// </summary>
    public void UpdateVignette(float pct)
    {
        if (_vignette == null) return;

        _isResetting = false;
        _analyzeAdd = Mathf.Lerp(0f, _maxVignetteIntensity, pct / 100f);
        ApplyVignette();
    }

    /// <summary>
    /// 解析リセット時に呼ぶ。解析 Vignette をフェードアウトする。
    /// </summary>
    public void ResetVignette()
    {
        if (_vignette == null) return;
        _isResetting = true;
    }

    /// <summary>
    /// 敵に発見された際のヴィネット強度を設定する。DetectionCameraEffects から毎フレーム呼ぶ。
    /// </summary>
    public void SetDetectionVignette(float intensity)
    {
        _detectionAdd = intensity;
    }

    /// <summary>
    /// 隠れ状態を設定する。true でフェードイン、false でフェードアウト。
    /// </summary>
    public void SetHiding(bool isHiding, float intensity, float fadeSpeed)
    {
        _isHiding        = isHiding;
        _hidingTarget    = isHiding ? intensity : 0f;
        _hidingFadeSpeed = fadeSpeed;
    }

    private void ApplyVignette()
    {
        // フェードが完了している割合に応じてパルス振幅をスケール（フェード中は揺れない）
        float fadeRatio = _hidingTarget > 0f ? _hidingCurrent / _hidingTarget : 0f;
        float pulse = _isHiding
            ? Mathf.Sin(Time.time * _hidingPulseFrequency * Mathf.PI * 2f) * _hidingPulseAmplitude * fadeRatio
            : 0f;
        _vignette.intensity.value = Mathf.Min(_baseIntensity + _analyzeAdd + _hidingCurrent + _detectionAdd + pulse, 1f);
    }
}