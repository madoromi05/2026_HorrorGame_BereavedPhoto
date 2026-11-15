using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵の検知状態を管理し、解析進行率を更新する
/// 解析率は敵のInstanceIDをキーとしたDictionaryで敵ごとに保存する
/// 検知・非検知の切り替えは外部から SetEnemyInRange() で通知する
/// 注目中の敵の切り替えは SetCurrentEnemy() で通知する
/// </summary>
public class EnemyAnalyzer : MonoBehaviour
{
    public const float kCompletePercent = 100f;	// 解析の完了。解析率はこの値まで上がる

    [SerializeField] private float _analyzeSpeed = 0.3f;
    [SerializeField] private AnalyzerUI _analyzerUI;

    // 解析率は敵の「種類（GhostType）」ごとに共有する
    // 同じ種類の敵が複数体いても解析率は1つにまとまり、代表1体を100%にすればその種類は完了扱い
    private readonly Dictionary<EnemyType, float> _analyzePercents = new();
    private EnemyType? _currentGhostType = null;

    private readonly HashSet<EnemyType> _completedTypes = new();	// 100%到達を検知して「その種類の敵を消す」処理を一度だけ実行するための記録

    // 現在ターゲット中の種類の解析率。カメラを外すと 0 を返す（UI更新用）
    public float AnalyzePercent =>
        _currentGhostType.HasValue && _analyzePercents.TryGetValue(_currentGhostType.Value, out var v) ? v : 0f;

    /// <summary>
    /// 解析完了判定。EnemyType の全種類が 100% に達していれば true
    /// 解析率は種類ごとに共有されるため、各種類につき1体でも 100% にすればその種類は完了扱い
    /// </summary>
    public bool IsComplete
    {
        get
        {
            foreach (EnemyType type in System.Enum.GetValues(typeof(EnemyType)))
            {
                // 未解析の種類が1つでもあれば未完了
                if (GetAnalyzePercent(type) < kCompletePercent)
                    return false;
            }
            return true;
        }
    }

    private bool _enemyInRange     = false;
    private bool _isAiming         = false;

    public void SetAiming(bool isAiming)
    {
        _isAiming = isAiming;
    }

    /// <summary>
    /// 現在注目している敵を設定する。nullなら未ターゲット状態
    /// 敵の GhostType を解析率のキーにするため、GhostIdentity を持たない対象は
    /// 解析不可（null）として扱う
    /// </summary>
    public void SetCurrentEnemy(GameObject enemy)
    {
        var ghost = enemy != null ? enemy.GetComponent<GhostIdentity>() : null;
        _currentGhostType = ghost?.GhostType;
    }

    private void Awake()
    {
        DebugCustom.ValidateFields(this,
            (nameof(_analyzerUI), _analyzerUI));
    }

    private void Update()
    {
        if (!_isAiming) return;

        if (!_currentGhostType.HasValue)
        {
            ShowNoTarget();
            return;
        }

        var type = _currentGhostType.Value;
        float current = AnalyzePercent;
        bool isComplete = current >= kCompletePercent;
        bool isAnalyzing = false;

        if (!isComplete)
        {
            if (_enemyInRange)
            {
                current = Mathf.Min(kCompletePercent, current + _analyzeSpeed * Time.deltaTime);
                isAnalyzing = true;
            }

            _analyzePercents[type] = current;
            isComplete = current >= kCompletePercent;
        }

        ShowProgress(current, isComplete, isAnalyzing);
        if (isComplete) CompleteTypeOnce(type);
    }

    private void ShowNoTarget()	// 狙っている敵がいないときの UI
    {
        _analyzerUI.OnAnalyzeUpdate(0f);
        _analyzerUI.SetAnalyzingState(false);
        _analyzerUI.SetCompleteState(false);
    }

    private void ShowProgress(float current, bool isComplete, bool isAnalyzing)
    {
        _analyzerUI.OnAnalyzeUpdate(current);
        // [Task]_vignetteController.UpdateVignette(current);

        _analyzerUI.SetCompleteState(isComplete);
        _analyzerUI.SetAnalyzingState(isAnalyzing && !isComplete);
    }

    /// <summary>
    /// その種類が初めて完了したときだけ、SE を鳴らして敵を退場させ、残りの種類を速くする
    /// 敵の消去処理そのものは EnemyController の責務に委譲する
    /// </summary>
    private void CompleteTypeOnce(EnemyType type)
    {
        if (!_completedTypes.Add(type)) return;

        AudioManager.Instance?.PlaySe(SeType.AnalysisComplete);
        EnemyController.DespawnByType(type);
        BoostRemainingEnemies();
    }

    /// <summary>
    /// まだ解析が完了していない種類の敵の移動速度を上げる
    /// 1種類倒すごとに残りが速くなり、緊張感を高める演出
    /// 実際の速度値は各 EnemyController の _speedAfterAnalysis で設定する
    /// </summary>
    private void BoostRemainingEnemies()
    {
        foreach (EnemyType type in System.Enum.GetValues(typeof(EnemyType)))
        {
            // すでに退場済み（完了済み）の種類は対象外
            if (_completedTypes.Contains(type)) continue;
            EnemyController.ApplySpeedAfterAnalysisByType(type);
        }
    }

    // 敵が解析範囲内にいるかどうかを外部から通知する
    public void SetEnemyInRange(bool inRange)
    {
        _enemyInRange = inRange;
    }

    // エイムを外したときの状態リセット。種類ごとの解析率は保持される
    public void ResetAimState()
    {
        _currentGhostType = null;
        _enemyInRange    = false;
        _isAiming        = false;
        _analyzerUI.ResetFields();
        // [Task]_vignetteController.ResetVignette();
    }

    // 指定した種類の敵の解析率を返す
    public float GetAnalyzePercent(EnemyType type)
        => _analyzePercents.TryGetValue(type, out var v) ? v : 0f;

    /// <summary>
    /// 指定した種類の解析を強制的に100%完了させるデバッグ用メソッド
    /// 実ゲームの完了時と同じく、初回完了時にSE再生とその種類の敵の退場も行う
    /// </summary>
    public void DebugCompleteType(EnemyType type)
    {
        _analyzePercents[type] = kCompletePercent;
        CompleteTypeOnce(type);
        DebugCustom.Log($"[Debug] 解析強制完了: {type}");
    }
}