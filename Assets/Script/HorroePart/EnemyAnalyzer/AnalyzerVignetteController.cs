using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 解析率に応じて Post Processing の Vignette 強度を制御する。
/// AnalyzerUI とは責務が異なる（カメラ効果）ため独立したクラスとして管理する。
/// </summary>
public class AnalyzerVignetteController : MonoBehaviour
{
    [SerializeField] private Volume volume;
    [SerializeField] private float maxVignetteIntensity = 0.6f;
    [SerializeField] private float resetFadeSpeed = 2f; // リセット時に元に戻る速さ

    private Vignette vignette;
    private bool isResetting = false;

    private void Awake()
    {
        // VolumeProfile に Vignette が存在しない場合は機能しない
        if (!volume.profile.TryGet(out vignette))
            Debug.LogWarning("[AnalyzerVignetteController] VolumeProfile に Vignette が見つかりません。");
    }

    private void Update()
    {
        if (!isResetting || vignette == null) return;

        float current = vignette.intensity.value;
        vignette.intensity.value = Mathf.MoveTowards(current, 0f, resetFadeSpeed * Time.deltaTime);

        if (Mathf.Approximately(vignette.intensity.value, 0f))
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
        vignette.intensity.value = Mathf.Lerp(0f, maxVignetteIntensity, pct / 100f);
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