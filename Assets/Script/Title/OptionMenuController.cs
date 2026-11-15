using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// タイトル・ゲーム中共用のオプションパネル。
/// タブ（音声 / 操作）をマウスで切替え、設定は PlayerPrefs で永続化する。
/// ここはパネルの開閉の窓口（Facade）。音量スライダーは VolumeSliders、感度スライダーは SensitivitySlider、
/// 音量を音へ映すのは VolumeApplier、SE の試し鳴らしは SePreviewDebouncer が持つ
/// </summary>
public class OptionMenuController : MonoBehaviour
{
    [Header("ルートパネル")]
    [SerializeField] private GameObject _optionPanel;

    [Header("音声")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _seSlider;

    [Tooltip("SE スライダー操作後、この秒数だけ動きが止まったら SE を再生する")]
    [SerializeField] private float _sePlayDelay = 0.3f;
    [SerializeField] private SeType _testSeType = SeType.ItemPickup;

    [Header("操作")]
    [SerializeField] private Slider _sensitivitySlider;
    [SerializeField] private GameObject _controlImagePanel;

    [Header("閉じるボタン")]
    [SerializeField] private Button _closeButton;

    private bool _initialized;
    private VolumeSliders _volumeSliders;
    private SensitivitySlider _sensitivity;
    private SePreviewDebouncer _sePreview;


    private void Start()
    {
        if (_optionPanel != null)
            _optionPanel.SetActive(false);

        Initialize();
    }

    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        _closeButton?.onClick.AddListener(CloseOption);

        _sePreview = new SePreviewDebouncer(this);
        _volumeSliders = new VolumeSliders(_masterSlider, _bgmSlider, _seSlider, RequestSePreview);
        _sensitivity = new SensitivitySlider(_sensitivitySlider);

        _volumeSliders.Bind();
        _sensitivity.Bind();	// 感度の上限もここで合わせる。音量スライダーとは別の Slider なので、音量より後でも結果は同じ

        VolumeApplier.ApplySaved();
    }

    // ---- 公開 API ----

    public void OpenOption()
    {
        Initialize();
        RefreshSliders();
        if (_optionPanel != null)
            _optionPanel.SetActive(true);
    }

    public void CloseOption()
    {
        if (_optionPanel != null)
            _optionPanel.SetActive(false);
    }

    public bool IsOpen => _optionPanel != null && _optionPanel.activeSelf;


    // ヘルパー

    private void RequestSePreview() => _sePreview.Request(_sePlayDelay, _testSeType);	// Inspector の値を呼ぶたびに読む

    private void RefreshSliders()
    {
        _volumeSliders.Refresh();
        _sensitivity.Refresh();
    }
}
