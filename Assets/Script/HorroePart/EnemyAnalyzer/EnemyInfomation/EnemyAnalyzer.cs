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

    private bool _enemyInRange = false;
    private bool _isAiming = false;

    public void SetAiming(bool isAiming) => _isAiming = isAiming;

    /// <summary>現在注目している敵を設定する。nullなら未ターゲット状態。</summary>
    public void SetCurrentEnemy(GameObject enemy)
    {
        _currentEnemyId = enemy != null ? (int?)enemy.GetInstanceID() : null;
    }

    private void Update()
    {
        if (!_isAiming) return;

        if (!_currentEnemyId.HasValue)
        {
            _analyzerUI.OnAnalyzeUpdate(0f);
            _vignetteController.UpdateVignette(0f);
            return;
        }

        float current = AnalyzePercent;

        if (current < 100f)
        {
            if (_enemyInRange)
                current = Mathf.Min(100f, current + _analyzeSpeed * Time.deltaTime);
            else if (_decaySpeed > 0f)
                current = Mathf.Max(0f, current - _decaySpeed * Time.deltaTime);

            _analyzePercents[_currentEnemyId.Value] = current;
        }

        _analyzerUI.OnAnalyzeUpdate(current);
        _vignetteController.UpdateVignette(current);
    }

    /// <summary>敵が解析範囲内にいるかどうかを外部から通知する。</summary>
    public void SetEnemyInRange(bool inRange)
    {
        _enemyInRange = inRange;
    }

    /// <summary>エイムを外したときの状態リセット。敵ごとの解析率は保持される。</summary>
    public void Reset()
    {
        _currentEnemyId = null;
        _enemyInRange = false;
        _isAiming = false;
        _analyzerUI.ResetFields();
        _vignetteController.ResetVignette();
    }

    /// <summary>指定InstanceIDの敵の解析率を返す（デバッグUI用）。</summary>
    public float GetAnalyzePercent(int instanceId)
        => _analyzePercents.TryGetValue(instanceId, out var v) ? v : 0f;

    /// <summary>
    /// デバッグ: 解析を強制完了させる。
    /// ターゲット中の敵がいればその敵のみ。いなければシーン上の全敵を完了させる。
    /// </summary>
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