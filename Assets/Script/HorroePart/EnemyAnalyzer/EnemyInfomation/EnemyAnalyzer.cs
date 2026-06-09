using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵の検知状態を管理し、解析進行率を更新する。
/// 解析率は敵のInstanceIDをキーとしたDictionaryで敵ごとに保存する。
/// 検知・非検知の切り替えは外部から SetEnemyInRange() で通知する。
/// 注目中の敵の切り替えは SetCurrentEnemy() で通知する。
/// </summary>
public class EnemyAnalyzer : MonoBehaviour
{
    [SerializeField] private float _analyzeSpeed = 0.3f;
    [SerializeField] private float _decaySpeed = 0.5f;
    [SerializeField] private AnalyzerUI _analyzerUI;
    [SerializeField] private AnalyzerVignetteController _vignetteController;

    private readonly Dictionary<int, float> _analyzePercents = new();
    private int? _currentEnemyId = null;

    /// <summary>現在ターゲット中の敵の解析率。カメラを外すと 0 を返す（UI更新用）。</summary>
    public float AnalyzePercent =>
        _currentEnemyId.HasValue && _analyzePercents.TryGetValue(_currentEnemyId.Value, out var v) ? v : 0f;

    /// <summary>
    /// 解析完了判定。いずれかの敵が 100% に達していれば true。
    /// カメラを外した状態でも正しく判定されるよう、辞書全体を確認する。
    /// </summary>
    public bool IsComplete
    {
        get
        {
            foreach (var v in _analyzePercents.Values)
                if (v >= 100f) return true;
            return false;
        }
    }

    private bool _enemyInRange     = false;
    private bool _isAiming         = false;
    private bool _wasComplete      = false;
    private bool _mapEnemyInView   = false;

    public void SetAiming(bool isAiming) => _isAiming = isAiming;

    /// <summary>MapWanderer（徘徊敵）がカメラ範囲内にいるかを設定する。解析不可UIの制御に使う。</summary>
    public void SetMapEnemyInView(bool inView) => _mapEnemyInView = inView;

    /// <summary>現在注目している敵を設定する。nullなら未ターゲット状態。</summary>
    public void SetCurrentEnemy(GameObject enemy)
    {
        _currentEnemyId = enemy != null ? (int?)enemy.GetInstanceID() : null;
    }

    private void Awake()
    {
        DebugCustom.ValidateFields(this,
            (nameof(_analyzerUI), _analyzerUI),
            (nameof(_vignetteController), _vignetteController));
    }

    private void Update()
    {
        if (!_isAiming) return;

        // MapWanderer（通路徘徊敵）を狙っているときは解析不可UIを表示して終了
        if (_mapEnemyInView)
        {
            _analyzerUI.OnAnalyzeUpdate(0f);
            _vignetteController.UpdateVignette(0f);
            _analyzerUI.SetAnalyzingState(false);
            _analyzerUI.SetCompleteState(false);
            _analyzerUI.SetNotAnalyzableState(true);
            return;
        }
        _analyzerUI.SetNotAnalyzableState(false);

        if (!_currentEnemyId.HasValue)
        {
            _analyzerUI.OnAnalyzeUpdate(0f);
            _vignetteController.UpdateVignette(0f);
            _analyzerUI.SetAnalyzingState(false);
            _analyzerUI.SetCompleteState(false);
            return;
        }

        float current = AnalyzePercent;
        bool isComplete = current >= 100f;
        bool isAnalyzing = false;

        if (!isComplete)
        {
            if (_enemyInRange)
            {
                current = Mathf.Min(100f, current + _analyzeSpeed * Time.deltaTime);
                isAnalyzing = true;
            }
            else if (_decaySpeed > 0f)
            {
                current = Mathf.Max(0f, current - _decaySpeed * Time.deltaTime);
            }

            _analyzePercents[_currentEnemyId.Value] = current;
            isComplete = current >= 100f;
        }

        _analyzerUI.OnAnalyzeUpdate(current);
        _vignetteController.UpdateVignette(current);

        _analyzerUI.SetCompleteState(isComplete);
        _analyzerUI.SetAnalyzingState(isAnalyzing && !isComplete);

        if (isComplete && !_wasComplete)
            AudioManager.Instance?.PlaySe(SeType.AnalysisComplete);
        _wasComplete = isComplete;
    }

    /// 敵が解析範囲内にいるかどうかを外部から通知する。
    public void SetEnemyInRange(bool inRange)
    {
        _enemyInRange = inRange;
    }

    /// エイムを外したときの状態リセット。敵ごとの解析率は保持される。
    public void Reset()
    {
        _currentEnemyId  = null;
        _enemyInRange    = false;
        _isAiming        = false;
        _wasComplete     = false;
        _mapEnemyInView  = false;
        _analyzerUI.ResetFields();
        _vignetteController.ResetVignette();
    }

    /// >指定InstanceIDの敵の解析率を返す
    public float GetAnalyzePercent(int instanceId)
        => _analyzePercents.TryGetValue(instanceId, out var v) ? v : 0f;
    public void DebugForceComplete()
    {
        if (_currentEnemyId.HasValue)
        {
            _analyzePercents[_currentEnemyId.Value] = 100f;
            DebugCustom.Log("[Debug] 解析強制完了（現在のターゲット）");
            return;
        }

        var enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        if (enemies.Length == 0)
        {
            DebugCustom.LogWarning("[Debug] 解析対象の敵が見つかりません（カメラで敵を狙ってから実行してください）");
            return;
        }
        foreach (var e in enemies)
            _analyzePercents[e.gameObject.GetInstanceID()] = 100f;
        DebugCustom.Log($"[Debug] 解析強制完了（{enemies.Length}体）");
    }

}