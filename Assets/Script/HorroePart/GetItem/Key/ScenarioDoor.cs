using HorrorGame.Item;
using HorrorGame.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HorrorGame.Interaction
{
    /// <summary>
    /// Door プレハブにアタッチするコンポーネント。
    /// 以下の条件をすべて満たしたとき ScenarioPart シーンへ遷移する：
    ///   1. 対象の EnemyAnalyzer の解析率が 100%
    ///   2. _requiredKey に対応する鍵を Inventory に持っている
    ///
    /// Door はランタイム生成のため Inspector アサインは行わない。
    /// Inventory・EnemyAnalyzer・InteractFeedbackUI は
    /// 初回アクセス時に FindFirstObjectByType でキャッシュして取得する。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ScenarioDoor : MonoBehaviour, IInteractable
    {
        private const string kScenarioSceneName = "ScenarioPart";

        // 脱出条件の状態
        private enum ExitCondition
        {
            Ready,          // 全条件クリア → シーン遷移
            NeedAnalysis,   // 解析未完了のみ
            NeedKey,        // 鍵なしのみ
            NeedBoth,       // 解析未完了 + 鍵なし
        }

        [Tooltip("脱出に必要な鍵の種類（Door ごとに Inspector で設定）")]
        [SerializeField] private ItemType _requiredKey;

        // ---- ランタイム参照（初回アクセス時に自動取得・キャッシュ） ----
        private Inventory          _inventory;
        private EnemyAnalyzer      _enemyAnalyzer;
        private InteractFeedbackUI _feedbackUI;

        private Inventory          Inventory   => _inventory    ??= FindFirstObjectByType<Inventory>();
        private EnemyAnalyzer      Analyzer    => _enemyAnalyzer ??= FindFirstObjectByType<EnemyAnalyzer>();
        private InteractFeedbackUI FeedbackUI  => _feedbackUI   ??= FindFirstObjectByType<InteractFeedbackUI>();

        // ---- IInteractable 実装 ----

        // 常にクリック可能。条件チェックは OnInteract() で行う
        public bool CanInteract => true;

        // 条件達成済みなら「外に出る」、未達なら「調べる」
        public string HintText => EvaluateCondition() == ExitCondition.Ready
            ? "【E】外に出る"
            : "【E】ドアを調べる";

        public void OnInteract()
        {
            var cond = EvaluateCondition();

            if (cond == ExitCondition.Ready)
            {
                SceneManager.LoadScene(kScenarioSceneName);
                return;
            }

            // 条件未達 → 状況に応じたメッセージを表示
            FeedbackUI?.Show(GetMessage(cond));
        }

        // ---- 内部ロジック ----

        private ExitCondition EvaluateCondition()
        {
            bool analysisOk = Analyzer  != null && Analyzer.IsComplete;
            bool hasKey     = Inventory != null && Inventory.HasItem(_requiredKey);

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
                "脱出に必要な鍵がありません。",
            ExitCondition.NeedBoth =>
                "敵の解析が完了しておらず、\n脱出に必要な鍵もありません。",
            _ => ""
        };
    }
}
