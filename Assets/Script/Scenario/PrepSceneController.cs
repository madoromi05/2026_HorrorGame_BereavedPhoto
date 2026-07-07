using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PrepScene にアタッチするコントローラー。
/// GameProgressManager の現在ステージ（Prep1 / Prep2）に応じた家族情報を表示し、
/// 「調査開始」ボタンで次のホラーパートへ遷移する。
/// </summary>
public class PrepSceneController : MonoBehaviour
{
    [System.Serializable]
    public class FamilyInfo
    {
        [Header("調査対象")]
        public string TargetName = "不明";
        [TextArea(2, 4)]
        public string Relation = ""; // 続柄・関係性
        [TextArea(3, 6)]
        public string BackgroundText = ""; // 人物の背景情報
        [TextArea(3, 6)]
        public string InvestigationNote = ""; // 調査メモ・注意事項

        [Header("画像")]
        public Sprite PortraitSprite;   // 人物の肖像
        public Sprite LocationSprite;   // 場所の写真など

        [Header("ボタンテキスト")]
        public string StartButtonLabel = "調査開始";
    }

    [Header("ステージ別データ")]
    [Tooltip("Prep1（ホラー1前・母の幽霊）で表示する家族情報")]
    [SerializeField] private FamilyInfo _prep1Info;

    [Tooltip("Prep2（ホラー2前・父の幽霊）で表示する家族情報")]
    [SerializeField] private FamilyInfo _prep2Info;

    [Header("UI要素")]
    [SerializeField] private TextMeshProUGUI _targetNameText;
    [SerializeField] private TextMeshProUGUI _relationText;
    [SerializeField] private TextMeshProUGUI _backgroundText;
    [SerializeField] private TextMeshProUGUI _investigationNoteText;
    [SerializeField] private Image           _portraitImage;
    [SerializeField] private Image           _locationImage;
    [SerializeField] private Button          _startButton;
    [SerializeField] private TextMeshProUGUI _startButtonText;

    [Header("ステージ表示")]
    [Tooltip("「準備パート 1/2」などを表示するテキスト（任意）")]
    [SerializeField] private TextMeshProUGUI _stageLabel;

    private void Start()
    {
        // ホラーシーンが1つに統合され準備パートは廃止されたため、
        // このシーンが使われる場合は既定の調査情報のみを表示する。
        ApplyInfo(_prep1Info);

        if (_stageLabel != null)
            _stageLabel.text = "調査準備";

        _startButton?.onClick.AddListener(OnStartPressed);
    }

    private void OnDestroy()
    {
        _startButton?.onClick.RemoveListener(OnStartPressed);
    }

    private void ApplyInfo(FamilyInfo info)
    {
        if (info == null) return;

        if (_targetNameText      != null) _targetNameText.text       = info.TargetName;
        if (_relationText        != null) _relationText.text         = info.Relation;
        if (_backgroundText      != null) _backgroundText.text       = info.BackgroundText;
        if (_investigationNoteText != null) _investigationNoteText.text = info.InvestigationNote;
        if (_startButtonText     != null) _startButtonText.text      = info.StartButtonLabel;

        if (_portraitImage  != null && info.PortraitSprite  != null)
            _portraitImage.sprite  = info.PortraitSprite;

        if (_locationImage  != null && info.LocationSprite  != null)
            _locationImage.sprite  = info.LocationSprite;
    }

    private void OnStartPressed()
    {
        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.LoadNextScene();
        else
            DebugCustom.LogWarning("[PrepSceneController] GameProgressManager not found");
    }
}
