using HorrorGame.Interaction;
using HorrorGame.Item;
using HorrorGame.UI;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ゲームの脱出ドア。
/// 以下の条件をすべて満たしたときのみ脱出できる：
///   1. 対象の敵（EnemyAnalyzer）の解析率が 100%
///   2. 指定された鍵アイテムを所持している
///
/// 条件を満たしていない状態でクリックすると、
/// ExitCondition の状態に応じたメッセージを InteractFeedbackUI に表示する。
/// </summary>
public class ExitDoor : MonoBehaviour, IInteractable
{
    // ---- 脱出条件の状態 ----
    private enum ExitCondition
    {
        Ready,          // 全条件クリア → 脱出可能
        NeedAnalysis,   // 解析未完了のみ
        NeedKey,        // 鍵なしのみ
        NeedBoth,       // 解析未完了 + 鍵なし
    }

    [Header("脱出条件")]
    [SerializeField] private EnemyAnalyzer _enemyAnalyzer;
    [SerializeField] private ItemType _requiredKey = ItemType.KeyFather;
    [SerializeField] private Inventory _inventory;

    [Header("UI")]
    [SerializeField] private InteractFeedbackUI _feedbackUI;

    [Header("脱出イベント")]
    [SerializeField] private UnityEvent _onExit;

    // ---- IInteractable 実装 ----

    // 常にインタラクト可能（条件チェックは OnInteract 内で行う）
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
            return;
        }

        // 条件未達 → 状況に応じたメッセージを表示
        _feedbackUI?.Show(GetMessage(cond));
    }

    // ---- 内部ロジック ----

    /// <summary>現在の条件達成状況を返す。</summary>
    private ExitCondition EvaluateCondition()
    {
        bool analysisOk = _enemyAnalyzer != null && _enemyAnalyzer.IsComplete;
        bool hasKey     = _inventory     != null && _inventory.HasItem(_requiredKey);

        if (!analysisOk && !hasKey) return ExitCondition.NeedBoth;
        if (!analysisOk)            return ExitCondition.NeedAnalysis;
        if (!hasKey)                return ExitCondition.NeedKey;
        return ExitCondition.Ready;
    }

    /// <summary>ExitCondition に対応するメッセージを返す。</summary>
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
