using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEngine;

/// <summary>
/// 敵の検知状態を管理し、解析進行率を更新する。
/// 検知・非検知の切り替えは外部から SetEnemyInRange() で通知する。
/// </summary>
public class EnemyAnalyzer : MonoBehaviour
{
    // ---- 調整パラメータ ----
    [SerializeField] private float _analyzeSpeed = 0.1f;  // %/秒
    [SerializeField] private float _decaySpeed = 0f;     // 範囲外で減衰させたい場合は正値に
    [SerializeField] private AnalyzerUI _analyzerUI;
    [SerializeField] private AnalyzerVignetteController _vignetteController;

    public float AnalyzePercent { get; private set; } = 0f;
    public bool IsComplete => AnalyzePercent >= 100f;

    private bool _enemyInRange = false;
    private bool _isAiming = false;

    public void SetAiming(bool isAiming) => _isAiming = isAiming;
    private void Update()
    {
        if (IsComplete) return;
        if (!_isAiming) return;
        if (_enemyInRange)
            AnalyzePercent = Mathf.Min(100f, AnalyzePercent + _analyzeSpeed * Time.deltaTime);
        else if (_decaySpeed > 0f)
            AnalyzePercent = Mathf.Max(0f, AnalyzePercent - _decaySpeed * Time.deltaTime);

        _analyzerUI.OnAnalyzeUpdate(AnalyzePercent);
        _vignetteController.UpdateVignette(AnalyzePercent);
    }

    /// <summary>敵が解析範囲に入ったか否かを外部から通知する。</summary>
    public void SetEnemyInRange(bool inRange)
    {
        _enemyInRange = inRange;
    }

    // 構えを解除したときの処理
    public void Reset()
    {
        AnalyzePercent = 0f;
        _enemyInRange = false;
        _isAiming = false;
        _analyzerUI.ResetFields();
        _vignetteController.ResetVignette();
    }
}