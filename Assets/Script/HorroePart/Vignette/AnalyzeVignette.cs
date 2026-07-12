using UnityEngine;

/// <summary>
/// 敵の解析率に応じて加算されるビネット。
/// 解析中は率に比例して強め、リセット時はフェードアウトする。
/// EnemyAnalyzer から解析率を受け取る（UpdateVignette / ResetVignette）。
/// </summary>
public class AnalyzeVignette : MonoBehaviour, IVignetteSource
{
    private const float kFullPercent = 100f;   // 解析率の最大値（%）

    [SerializeField] private float _maxIntensity   = 0.4f;   // 解析率100%のときの追加強度
    [SerializeField] private float _resetFadeSpeed = 3f;     // リセット時に元へ戻る速さ

    private float _current;
    private bool  _isResetting;

    public float Intensity => _current;

    /// <summary>
    /// 解析率（0〜100）を受け取りビネット強度へ反映する。EnemyAnalyzer の Update から毎フレーム呼ぶ想定。
    /// </summary>
    public void UpdateVignette(float pct)
    {
        _isResetting = false;
        _current = Mathf.Lerp(0f, _maxIntensity, pct / kFullPercent);
    }

    /// <summary>解析リセット時に呼ぶ。ビネットをフェードアウトする。</summary>
    public void ResetVignette()
    {
        _isResetting = true;
    }

    private void Update()
    {
        if (!_isResetting) return;

        _current = Mathf.MoveTowards(_current, 0f, _resetFadeSpeed * Time.deltaTime);
        if (Mathf.Approximately(_current, 0f))
            _isResetting = false;
    }
}
