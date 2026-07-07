using HorrorGame.Interaction;
using HorrorGame.Item;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// @madoromi _onExit は互換性のために残してある。
/// </summary>
[RequireComponent(typeof(Collider))]
public class ExitDoor : MonoBehaviour, IInteractable
{
    private enum ExitCondition
    {
        Ready,
        NeedAnalysis,
        NeedKey,
        NeedBoth,
    }

    [Header("脱出条件")]
    [SerializeField] private ItemType _requiredKey = ItemType.KeyMother;

    [Header("参照（未設定の場合はシーン内から自動検索）")]
    [SerializeField] private EnemyAnalyzer      _enemyAnalyzer;
    [SerializeField] private Inventory          _inventory;

    [Header("脱出時の追加イベント（任意）")]
    [SerializeField] private UnityEvent _onExit;

    // SerializeField が未設定なら FindFirstObjectByType でキャッシュして取得
    private EnemyAnalyzer      Analyzer   => _enemyAnalyzer ??= FindFirstObjectByType<EnemyAnalyzer>();
    private Inventory          Inventory  => _inventory     ??= FindFirstObjectByType<Inventory>();
    private bool _isInteracting = false;

    // ---- IInteractable 実装 ----
    public bool CanInteract => true;
    public string HintText => EvaluateCondition() switch
    {
        ExitCondition.Ready   => "【E】外に出る",
        ExitCondition.NeedKey => "鍵が掛かってる...",
        _                     => "ドアを開けることができない霊を機材で解析しなければ...",
    };
    public void OnInteract()
    {
        if (_isInteracting) return;
        // 条件を満たさないときは HintText が理由を表示しているので何もしない。
                if (EvaluateCondition() != ExitCondition.Ready) return;
        
        _isInteracting = true;
        _onExit?.Invoke();
        
        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.LoadNextScene();
                else
            DebugCustom.LogWarning("[ExitDoor] GameProgressManager が見つかりません。シーン遷移できません。");

        _isInteracting = false;
    }

    private ExitCondition EvaluateCondition()
    {
        bool analysisOk = Analyzer != null && Analyzer.IsComplete;
        bool hasKey = Inventory != null && Inventory.HasItem(_requiredKey);

        if (!analysisOk && !hasKey) return ExitCondition.NeedBoth;
        if (!analysisOk) return ExitCondition.NeedAnalysis;
        if (!hasKey) return ExitCondition.NeedKey;
        return ExitCondition.Ready;
    }
}
