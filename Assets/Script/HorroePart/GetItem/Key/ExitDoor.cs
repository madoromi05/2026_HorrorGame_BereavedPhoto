using HorrorGame.Interaction;
using HorrorGame.Item;
using HorrorGame.UI;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
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

    private string _fallbackMessage = "";
    private float  _fallbackTimer   = 0f;
    private const float kFallbackDuration = 3f;
    private bool _isInteracting = false;

    private void Update()
    {
        if (_fallbackTimer > 0f)
            _fallbackTimer -= Time.deltaTime;
    }

    private void OnGUI()
    {
        if (_fallbackTimer <= 0f || string.IsNullOrEmpty(_fallbackMessage)) return;

        var style = new GUIStyle(GUI.skin.box)
        {
            fontSize  = 22,
            alignment = TextAnchor.MiddleCenter,
            wordWrap  = true,
        };
        style.normal.textColor = Color.white;

        float w = 480f, h = 100f;
        GUI.Box(new Rect((Screen.width - w) * 0.5f, Screen.height * 0.65f, w, h),
                _fallbackMessage, style);
    }

    // ---- IInteractable 実装 ----

    public bool CanInteract => true;

    public string HintText => EvaluateCondition() == ExitCondition.Ready
        ? "【E】外に出る"
        : "【E】ドアを調べる";

    public void OnInteract()
    {
        if (_isInteracting) return;
        _isInteracting = true;

        var cond = EvaluateCondition();

        if (cond == ExitCondition.Ready)
        {
            _onExit?.Invoke();

            if (GameProgressManager.Instance != null)
                GameProgressManager.Instance.LoadNextScene();
            else
                DebugCustom.LogWarning("[ExitDoor] GameProgressManager が見つかりません。シーン遷移できません。");

            _isInteracting = false;
            return;
        }

        var message = GetMessage(cond);
        if (FeedbackUI != null)
        {
            FeedbackUI.Show(message);
        }
        else
        {
            // テストシーン用フォールバック: OnGUI で画面中央下部に表示
            _fallbackMessage = message;
            _fallbackTimer   = kFallbackDuration;
        }

        _isInteracting = false;
    }

    private ExitCondition EvaluateCondition()
    {
        // ホラーシーンは1つに統合されたため、必要な鍵は Inspector 設定値を使う。
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
            "まだ、帰るわけにはいかない...",
        ExitCondition.NeedKey =>
            "鍵が掛かってる...",
        ExitCondition.NeedBoth =>
            "まだ、帰るわけにはいかない...",
        _ => ""
    };
}
