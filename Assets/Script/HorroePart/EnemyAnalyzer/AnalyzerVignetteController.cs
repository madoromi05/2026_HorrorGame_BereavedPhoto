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
    [SerializeField] private float _maxVignetteIntensity = 0.8f; // 解析率100%のときの Vignette 強度
    [SerializeField] private float _resetFadeSpeed = 2f; // リセット時に元に戻る速さ

    private Vignette vignette;
    private float baseIntensity;
    private bool isResetting = false;

    private void Awake()
    {
        if (!_volume.profile.TryGet(out vignette))
        {
            Debug.LogWarning("[AnalyzerVignetteController] VolumeProfile に Vignette が見つかりません。");
            return;
        }
        baseIntensity = vignette.intensity.value;
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