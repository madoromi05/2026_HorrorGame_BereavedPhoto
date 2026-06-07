using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// タイトル・ゲーム中共用のオプションパネル。
/// タブ（音声 / 操作）をマウスで切替え、設定は PlayerPrefs で永続化する。
///
/// Inspector 設定目安:
///   _masterSlider / _bgmSlider / _seSlider : Min=0, Max=1, WholeNumbers=false
///   _sensitivitySlider                     : Min=0.02, Max=0.5, WholeNumbers=false
/// </summary>
public class OptionMenuController : MonoBehaviour
{
    // ---- PlayerPrefs keys ----
    public const string KeyMaster = "MasterVolume";
    public const string KeyBgm    = "BgmVolume";
    public const string KeySe     = "SeVolume";
    public const string KeySens   = "MouseSensitivity";

    [Header("ルートパネル")]
    [SerializeField] private GameObject _optionPanel;

    [Header("タブボタン")]
    [SerializeField] private Button _audioTabButton;
    [SerializeField] private Button _controlTabButton;

    [Header("タブ選択インジケーター（オプション）")]
    [SerializeField] private GameObject _audioTabIndicator;
    [SerializeField] private GameObject _controlTabIndicator;

    [Header("コンテンツパネル")]
    [SerializeField] private GameObject _audioPanel;
    [SerializeField] private GameObject _controlPanel;

    [Header("音声")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _seSlider;
    [SerializeField] private Button _seTestButton;
    [SerializeField] private SeType _testSeType = SeType.ItemPickup;

    [Header("音量ラベル（オプション）")]
    [SerializeField] private TMP_Text _masterLabel;
    [SerializeField] private TMP_Text _bgmLabel;
    [SerializeField] private TMP_Text _seLabel;

    [Header("操作")]
    [SerializeField] private Slider _sensitivitySlider;
    [SerializeField] private TMP_Text _sensitivityLabel;
    [SerializeField] private GameObject _controlImagePanel;

    [Header("閉じるボタン")]
    [SerializeField] private Button _closeButton;

    // ---- 状態 ----
    private bool _initialized;

    // ---- Unity ----

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

        _audioTabButton?.onClick.AddListener(ShowAudioTab);
        _controlTabButton?.onClick.AddListener(ShowControlTab);
        _closeButton?.onClick.AddListener(CloseOption);
        _seTestButton?.onClick.AddListener(OnSeTest);

        _masterSlider?.onValueChanged.AddListener(OnMasterChanged);
        _bgmSlider?.onValueChanged.AddListener(OnBgmChanged);
        _seSlider?.onValueChanged.AddListener(OnSeChanged);
        _sensitivitySlider?.onValueChanged.AddListener(OnSensitivityChanged);

        ApplySavedSettings();
        ShowAudioTab();
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

    // ---- タブ切替 ----

    private void ShowAudioTab()
    {
        SetTabActive(_audioPanel, _controlPanel,
                     _audioTabIndicator, _controlTabIndicator);
    }

    private void ShowControlTab()
    {
        SetTabActive(_controlPanel, _audioPanel,
                     _controlTabIndicator, _audioTabIndicator);
    }

    private void SetTabActive(GameObject show, GameObject hide,
                               GameObject showInd, GameObject hideInd)
    {
        if (show != null) show.SetActive(true);
        if (hide != null) hide.SetActive(false);
        if (showInd != null) showInd.SetActive(true);
        if (hideInd != null) hideInd.SetActive(false);
    }

    // ---- スライダーコールバック ----

    private void OnMasterChanged(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(KeyMaster, v);
        UpdateLabel(_masterLabel, v);
    }

    private void OnBgmChanged(float v)
    {
        AudioManager.Instance?.SetBgmVolume(v);
        PlayerPrefs.SetFloat(KeyBgm, v);
        UpdateLabel(_bgmLabel, v);
    }

    private void OnSeChanged(float v)
    {
        AudioManager.Instance?.SetSeVolume(v);
        PlayerPrefs.SetFloat(KeySe, v);
        UpdateLabel(_seLabel, v);
    }

    private void OnSeTest()
    {
        AudioManager.Instance?.PlaySe(_testSeType);
    }

    private void OnSensitivityChanged(float v)
    {
        PlayerPrefs.SetFloat(KeySens, v);
        if (_sensitivityLabel != null)
            _sensitivityLabel.text = v.ToString("F2");

        // ゲーム中なら即時反映
        foreach (var cam in FindObjectsByType<PlayerCamera>(FindObjectsSortMode.None))
            cam.SetSensitivity(v);
        foreach (var mover in FindObjectsByType<PlayerMover>(FindObjectsSortMode.None))
            mover.SetSensitivity(v);
    }

    // ---- 内部ヘルパー ----

    /// 保存済み設定をロードして各システムに反映する。
    private void ApplySavedSettings()
    {
        float master = PlayerPrefs.GetFloat(KeyMaster, 1f);
        float bgm    = PlayerPrefs.GetFloat(KeyBgm,    1f);
        float se     = PlayerPrefs.GetFloat(KeySe,     1f);
        float sens   = PlayerPrefs.GetFloat(KeySens,   0.1f);

        AudioListener.volume = master;
        AudioManager.Instance?.SetBgmVolume(bgm);
        AudioManager.Instance?.SetSeVolume(se);
    }

    /// パネルを開いたときにスライダー表示を最新値に合わせる。
    private void RefreshSliders()
    {
        float master = PlayerPrefs.GetFloat(KeyMaster, 1f);
        float bgm    = PlayerPrefs.GetFloat(KeyBgm,    1f);
        float se     = PlayerPrefs.GetFloat(KeySe,     1f);
        float sens   = PlayerPrefs.GetFloat(KeySens,   0.1f);

        _masterSlider?.SetValueWithoutNotify(master);
        _bgmSlider?.SetValueWithoutNotify(bgm);
        _seSlider?.SetValueWithoutNotify(se);
        _sensitivitySlider?.SetValueWithoutNotify(sens);

        UpdateLabel(_masterLabel, master);
        UpdateLabel(_bgmLabel,   bgm);
        UpdateLabel(_seLabel,    se);
        if (_sensitivityLabel != null)
            _sensitivityLabel.text = sens.ToString("F2");
    }

    private static void UpdateLabel(TMP_Text label, float v)
    {
        if (label != null)
            label.text = Mathf.RoundToInt(v * 100f) + "%";
    }
}
