using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// タイトル・ゲーム中共用のオプションパネル。
/// タブ（音声 / 操作）をマウスで切替え、設定は PlayerPrefs で永続化する。
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

    private bool _initialized;
    private Coroutine _seDebounce;


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
        AudioListener.volume = ToAppliedVolume(v);
        PlayerPrefs.SetFloat(KeyMaster, v);
    }

    private void OnBgmChanged(float v)
    {
        AudioManager.Instance?.SetBgmVolume(ToAppliedVolume(v));
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

    // ヘルパー

    /// <summary>
    /// スライダー値（0〜1、既定0.5）を実際の再生音量に変換する。
    /// 0.5 で以前のデフォルト音量（フル）と同じ音量になるよう、実音量はスライダー値の2倍とする
    /// （0.5〜1.0 はフル音量のまま。将来ブーストが必要になった場合の余地として上半分を残している）。
    /// </summary>
    private static float ToAppliedVolume(float sliderValue) => Mathf.Min(sliderValue * 2f, 1f);

    private void ApplySavedSettings()
    {
        AudioListener.volume = ToAppliedVolume(PlayerPrefs.GetFloat(KeyMaster, 0.5f));
        AudioManager.Instance?.SetBgmVolume(ToAppliedVolume(PlayerPrefs.GetFloat(KeyBgm, 0.5f)));
        AudioManager.Instance?.SetSeVolume(PlayerPrefs.GetFloat(KeySe, 0.5f));
    }

    private void RefreshSliders()
    {
        _masterSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat(KeyMaster, 0.5f));
        _bgmSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat(KeyBgm, 0.5f));
        _seSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat(KeySe, 0.5f));
        _sensitivitySlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat(KeySens, 0.5f));
    }
}
