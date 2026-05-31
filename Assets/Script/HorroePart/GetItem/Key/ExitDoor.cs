using HorrorGame.Interaction;
using HorrorGame.Item;
using HorrorGame.UI;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 脱出ドア（ScenarioDoor を統合した汎用版）。
/// 以下の条件をすべて満たしたとき GameProgressManager.LoadNextScene() で次のシーンへ遷移する：
///   1. いずれかの EnemyAnalyzer 解析率が 100%
///   2. _requiredKey に対応する鍵を Inventory に持っている
///
/// [SerializeField] が未設定の場合は FindFirstObjectByType でシーン内を自動検索する。
/// プロシージャルダンジョンでも手動配置シーンでもどちらでも使える。
/// _onExit は互換性のために残してある。
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
    [Tooltip("脱出に必要な鍵の種類。GameProgressManager が存在する場合はステージから自動判定するためこの値は無視される。")]
    [SerializeField] private ItemType _requiredKey = ItemType.KeyMother;

    [Header("参照（未設定の場合はシーン内から自動検索）")]
    [SerializeField] private EnemyAnalyzer      _enemyAnalyzer;
    [SerializeField] private Inventory          _inventory;
    [SerializeField] private InteractFeedbackUI _feedbackUI;

    [Header("脱出時の追加イベント（任意）")]
    [SerializeField] private UnityEvent _onExit;

    // SerializeField が未設定なら FindFirstObjectByType でキャッシュして取得
    private EnemyAnalyzer      Analyzer   => _enemyAnalyzer ??= FindFirstObjectByType<EnemyAnalyzer>();
    private Inventory          Inventory  => _inventory     ??= FindFirstObjectByType<Inventory>();
    private InteractFeedbackUI FeedbackUI => _feedbackUI    ??= FindFirstObjectByType<InteractFeedbackUI>();

    // ---- IInteractable 実装 ----

    public bool CanInteract => true;

    public string HintText => EvaluateCondition() == ExitCondition.Ready
        ? "【E】外に出る"
        : "【E】ドアを調べる";

    public void OnInteract()
    {
        var cond = EvaluateCondition();

        if (cond == ExitCondition.Ready)
        {
            _onExit?.Invoke();

            if (GameProgressManager.Instance != null)
                GameProgressManager.Instance.LoadNextScene();
            else
                Debug.LogWarning("[ExitDoor] GameProgressManager が見つかりません。シーン遷移できません。");
            return;
        }

        FeedbackUI?.Show(GetMessage(cond));
    }

    // ---- 内部ロジック ----

    /// <summary>
    /// 有効な鍵種別を返す。
    /// GameProgressManager が存在する場合はステージから自動判定するため、
    /// 単一の HorrorScene で両ステージに対応できる。
    /// </summary>
    private ItemType GetEffectiveKey()
    {
        var stage = GameProgressManager.Instance?.CurrentStage;
        if (stage == GameProgressManager.GameStage.Horror1) return ItemType.KeyMother;
        if (stage == GameProgressManager.GameStage.Horror2) return ItemType.KeyFather;
        return _requiredKey; // GameProgressManager がない場合は Inspector の設定値を使う
    }

    private ExitCondition EvaluateCondition()
    {
        bool analysisOk = Analyzer  != null && Analyzer.IsComplete;
        bool hasKey     = Inventory != null && Inventory.HasItem(GetEffectiveKey());

        if (!analysisOk && !hasKey) return ExitCondition.NeedBoth;
        if (!analysisOk)            return ExitCondition.NeedAnalysis;
        if (!hasKey)                return ExitCondition.NeedKey;
        return ExitCondition.Ready;
    }

    private static string GetMessage(ExitCondition cond) => cond switch
    {
        ExitCondition.NeedAnalysis =>
            "敵の解析が完了していません。\nカメラで敵をスキャンしてください。",
        ExitCondition.NeedKey =>
            "脱出に必要な鍵がありません。\n鍵を見つけてください。",
        ExitCondition.NeedBoth =>
            "敵の解析が完了しておらず、\n脱出に必要な鍵もありません。",
        _ => ""
    };
}
