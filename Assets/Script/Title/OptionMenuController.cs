using System.Collections;
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

    // ---- 状態 ----
    private bool _initialized;
    private Coroutine _seDebounce;

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

        _closeButton?.onClick.AddListener(CloseOption);

        _masterSlider?.onValueChanged.AddListener(OnMasterChanged);
        _bgmSlider?.onValueChanged.AddListener(OnBgmChanged);
        _seSlider?.onValueChanged.AddListener(OnSeChanged);
        _sensitivitySlider?.onValueChanged.AddListener(OnSensitivityChanged);

        ApplySavedSettings();
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


    // ---- スライダーコールバック ----

    private void OnMasterChanged(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(KeyMaster, v);
    }

    private void OnBgmChanged(float v)
    {
        AudioManager.Instance?.SetBgmVolume(v);
        PlayerPrefs.SetFloat(KeyBgm, v);
    }

    private void OnSeChanged(float v)
    {
        AudioManager.Instance?.SetSeVolume(v);
        PlayerPrefs.SetFloat(KeySe, v);

        // スライダーが止まった直後に1回だけ SE を再生（デバウンス）
        if (_seDebounce != null) StopCoroutine(_seDebounce);
        _seDebounce = StartCoroutine(PlaySeAfterDelay());
    }

    private IEnumerator PlaySeAfterDelay()
    {
        yield return new WaitForSecondsRealtime(_sePlayDelay);
        AudioManager.Instance?.PlaySe(_testSeType);
        _seDebounce = null;
    }

    private void OnSensitivityChanged(float v)
    {
        PlayerPrefs.SetFloat(KeySens, v);

        // ゲーム中なら即時反映
        foreach (var cam in FindObjectsByType<PlayerCamera>(FindObjectsSortMode.None))
            cam.SetSensitivity(v);
        foreach (var mover in FindObjectsByType<PlayerMover>(FindObjectsSortMode.None))
            mover.SetSensitivity(v);
    }

    // ---- 内部ヘルパー ----

    private void ApplySavedSettings()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(KeyMaster, 1f);
        AudioManager.Instance?.SetBgmVolume(PlayerPrefs.GetFloat(KeyBgm, 1f));
        AudioManager.Instance?.SetSeVolume(PlayerPrefs.GetFloat(KeySe, 0.5f));
    }

    private void RefreshSliders()
    {
        _masterSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat(KeyMaster, 1f));
        _bgmSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat(KeyBgm, 1f));
        _seSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat(KeySe, 0.5f));
        _sensitivitySlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat(KeySens, 0.1f));
    }
}
