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
    [SerializeField, Range(0f, 1f)] private float _permanentVignetteIntensity = 0.25f; // 常時表示するビネット強度
    [SerializeField] private float _maxVignetteIntensity = 0.8f; // 解析率100%のときの Vignette 追加強度
    [SerializeField] private float _resetFadeSpeed = 2f; // リセット時に元に戻る速さ

    private Vignette vignette;
    private float baseIntensity;
    private bool isResetting = false;

    private void Awake()
    {
        if (_volume == null)
        {
            Debug.LogError("[VignetteCtrl] _volume が Inspector で未アサインです。");
            return;
        }
        if (_volume.sharedProfile == null)
        {
            Debug.LogError("[VignetteCtrl] sharedProfile が null です。Volume に Profile を設定してください。");
            return;
        }

        _volume.profile = Instantiate(_volume.sharedProfile);

        if (!_volume.profile.TryGet(out vignette))
            vignette = _volume.profile.Add<Vignette>(overrides: true);

        vignette.active = true;
        vignette.intensity.overrideState = true;
        vignette.intensity.value = _permanentVignetteIntensity;
        baseIntensity = _permanentVignetteIntensity;
    }

    private void Update()
    {
        if (vignette == null || !isResetting) return;

        float current = vignette.intensity.value;
        vignette.intensity.value = Mathf.MoveTowards(current, baseIntensity, _resetFadeSpeed * Time.deltaTime);

        if (Mathf.Approximately(vignette.intensity.value, baseIntensity))
            isResetting = false;
    }

    /// <summary>
    /// 解析率（0〜100）を受け取り Vignette 強度に反映する。
    /// EnemyAnalyzer の Update から毎フレーム呼ばれることを想定。
    /// </summary>
    public void UpdateVignette(float pct)
    {
        if (vignette == null) return;

        isResetting = false;
        float analyzeAdd = Mathf.Lerp(0f, _maxVignetteIntensity, pct / 100f);
        vignette.intensity.value = Mathf.Min(baseIntensity + analyzeAdd, 1f);
    }

    /// <summary>
    /// 解析リセット時に呼ぶ。Vignette 強度を 0 に向けてフェードアウトする。
    /// </summary>
    public void ResetVignette()
    {
        if (vignette == null) return;
        isResetting = true;
    }
}