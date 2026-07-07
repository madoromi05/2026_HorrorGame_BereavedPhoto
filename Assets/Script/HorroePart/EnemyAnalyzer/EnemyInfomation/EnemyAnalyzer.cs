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

    [Header("クリア遷移")]
    [Tooltip("シーン内の全ての敵（母・父）を解析完了したら自動でクリア（次シーン）へ遷移する。" +
             "オフにすると脱出ドアからの遷移のみになる。")]
    [SerializeField] private bool _autoTransitionToClear = true;
    [Tooltip("解析完了からクリア遷移までの待機秒数")]
    [SerializeField] private float _clearTransitionDelay = 1.0f;

    // 解析率は敵の「種類（GhostType）」ごとに共有する。
    // 同じ種類の敵が複数体いても解析率は1つにまとまり、代表1体を100%にすればその種類は完了扱い。
    private readonly Dictionary<GhostType, float> _analyzePercents = new();
    private GhostType? _currentGhostType = null;

    private bool  _clearTriggered;
    private float _clearCheckTimer;
    private const float kClearCheckInterval = 0.5f;

    /// <summary>現在ターゲット中の種類の解析率。カメラを外すと 0 を返す（UI更新用）。</summary>
    public float AnalyzePercent =>
        _currentGhostType.HasValue && _analyzePercents.TryGetValue(_currentGhostType.Value, out var v) ? v : 0f;

    /// <summary>
    /// 解析完了判定。シーン内に存在する解析対象の敵（GhostIdentity 付き = 母・父の幽霊）の
    /// 全ての種類が 100% に達していれば true。解析率は種類ごとに共有されるため、
    /// 各種類につき1体でも 100% にすればその種類は完了扱いになる。
    /// 徘徊敵（MapWanderer）は GhostIdentity を持たないため判定対象外。
    /// </summary>
    public bool IsComplete
    {
        get
        {
            var ghosts = FindObjectsByType<GhostIdentity>(FindObjectsSortMode.None);
            if (ghosts.Length == 0) return false;

            foreach (var ghost in ghosts)
            {
                // 未解析の種類が1つでもあれば未完了
                if (GetAnalyzePercent(ghost.GhostType) < 100f)
                    return false;
            }
            return true;
        }
    }

    private bool _enemyInRange     = false;
    private bool _isAiming         = false;
    private bool _wasComplete      = false;
    private bool _mapEnemyInView   = false;

    public void SetAiming(bool isAiming) => _isAiming = isAiming;

    /// <summary>MapWanderer（徘徊敵）がカメラ範囲内にいるかを設定する。解析不可UIの制御に使う。</summary>
    public void SetMapEnemyInView(bool inView) => _mapEnemyInView = inView;

    /// <summary>
    /// 現在注目している敵を設定する。nullなら未ターゲット状態。
    /// 敵の GhostType を解析率のキーにするため、GhostIdentity を持たない対象は
    /// 解析不可（null）として扱う。
    /// </summary>
    public void SetCurrentEnemy(GameObject enemy)
    {
        _currentGhostType = enemy != null
            ? enemy.GetComponentInParent<GhostIdentity>()?.GhostType
            : null;
    }

    private void Awake()
    {
        DebugCustom.ValidateFields(this,
            (nameof(_analyzerUI), _analyzerUI),
            (nameof(_vignetteController), _vignetteController));
    }

    private void Update()
    {
        // 全敵の解析が完了したらクリア（次シーン）へ自動遷移する。
        // IsComplete はシーン検索を伴うため毎フレームではなく一定間隔で確認する。
        if (_autoTransitionToClear && !_clearTriggered)
        {
            _clearCheckTimer -= Time.deltaTime;
            if (_clearCheckTimer <= 0f)
            {
                _clearCheckTimer = kClearCheckInterval;
                if (IsComplete)
                {
                    _clearTriggered = true;
                    StartCoroutine(TransitionToClearCoroutine());
                }
            }
        }

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

        if (!_currentGhostType.HasValue)
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

            _analyzePercents[_currentGhostType.Value] = current;
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

    /// エイムを外したときの状態リセット。種類ごとの解析率は保持される。
    public void Reset()
    {
        _currentGhostType = null;
        _enemyInRange    = false;
        _isAiming        = false;
        _wasComplete     = false;
        _mapEnemyInView  = false;
        _analyzerUI.ResetFields();
        _vignetteController.ResetVignette();
    }

    /// >指定した種類の敵の解析率を返す
    public float GetAnalyzePercent(GhostType type)
        => _analyzePercents.TryGetValue(type, out var v) ? v : 0f;

    /// <summary>解析完了後、少し待ってからクリア（次シーン）へ遷移する。</summary>
    private System.Collections.IEnumerator TransitionToClearCoroutine()
    {
        DebugCustom.Log("[EnemyAnalyzer] 全敵の解析完了。クリアへ遷移します。", this);
        yield return new WaitForSeconds(_clearTransitionDelay);

        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.LoadNextScene();
        else
            DebugCustom.LogWarning("[EnemyAnalyzer] GameProgressManager が見つかりません。クリア遷移できません。", this);
    }
    public void DebugForceComplete()
    {
        // クリア条件がシーン内の全種類 100% になったため、存在する全種類をまとめて完了させる。
        var ghosts = FindObjectsByType<GhostIdentity>(FindObjectsSortMode.None);
        if (ghosts.Length == 0)
        {
            DebugCustom.LogWarning("[Debug] 解析対象の敵が見つかりません");
            return;
        }
        foreach (var g in ghosts)
            _analyzePercents[g.GhostType] = 100f;
        DebugCustom.Log($"[Debug] 解析強制完了（{_analyzePercents.Count}種類）");
    }

}