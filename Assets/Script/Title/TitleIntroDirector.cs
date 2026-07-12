using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タイトル画面のフルスクリーン演出を司るコンポーネント。
/// 責務は二つの画面トランジションのみ:
///   ・導入：ぼやけた状態から鮮明へフォーカスインする。
///   ・開始：ドアを開ける音とともに暗転させ、完了を呼び出し元へ通知する。
/// シーン遷移そのものは扱わず、演出の完了を <see cref="PlayStartSequence"/> の
/// コールバックで通知して呼び出し側に委譲する。
/// </summary>
public class TitleIntroDirector : MonoBehaviour
{
    // Image.material は共有アセットを指すため、_BlurSize をここから変更すると
    // .mat アセットごと書き換わってしまう。Awake で複製に差し替えて回避する。
    [Header("オーバーレイ")]
    [Tooltip("ぼかし用の全画面 Image。UITitleBlur マテリアルを割り当てる。")]
    [SerializeField] private Image _blurOverlay;
    [Tooltip("暗転用の全画面 CanvasGroup（黒 Image・alpha 0 開始）。")]
    [SerializeField] private CanvasGroup _darkenOverlay;

    [Header("パラメータ")]
    [Tooltip("導入開始時のぼかし強度（UITitleBlur の _BlurSize 単位）。")]
    [SerializeField] private float _startBlurAmount = 20f;
    [Tooltip("ぼかしが 0 になり鮮明になるまでの時間（秒）。")]
    [SerializeField] private float _focusInDuration = 4.0f;
    [Tooltip("暗転しきるまでの時間（秒）。")]
    [SerializeField] private float _darkenDuration = 1.2f;

    [Header("メニュー")]
    [Tooltip("タイトルメニュー UI 全体の CanvasGroup。導入のフォーカスイン後にフェードインする。")]
    [SerializeField] private CanvasGroup _menuGroup;
    [Tooltip("メニューがフェードインしきるまでの時間（秒）。")]
    [SerializeField] private float _menuFadeInDuration = 1.0f;

    // UITitleBlur が公開するぼかし強度プロパティ。
    private const string kBlurSizeProperty = "_BlurSize";
    private static readonly int kBlurSizeId = Shader.PropertyToID(kBlurSizeProperty);

    private Material _blurMaterial;   // _blurOverlay 専用に複製したインスタンス
    private bool     _startTriggered; // 二重起動防止（開始演出は一度きり）

    private void Awake()
    {
        if (_blurOverlay != null && _blurOverlay.material != null)
        {
            _blurMaterial = new Material(_blurOverlay.material);
            _blurOverlay.material = _blurMaterial;
        }

        if (_darkenOverlay != null)
            _darkenOverlay.alpha = 0f;

        // フォーカスイン完了までメニューは隠し、誤クリックも防ぐ。
        if (_menuGroup != null)
        {
            _menuGroup.alpha = 0f;
            _menuGroup.interactable = false;
            _menuGroup.blocksRaycasts = false;
        }
    }

    private void Start()
    {
        StartCoroutine(IntroSequence());
    }

    // 導入：ぼかし→鮮明化の後にメニュー UI をフェードインさせる。
    private IEnumerator IntroSequence()
    {
        yield return FocusIn();
        yield return FadeInMenu();
    }

    /// <summary>
    /// 開始演出を再生する。ドア音を鳴らしつつ暗転させ、暗転後に <paramref name="onComplete"/> を呼ぶ。
    /// 二重に呼ばれても最初の一回だけ実行する（ボタン連打でのシーン多重ロードを防ぐ）。
    /// </summary>
    public void PlayStartSequence(Action onComplete)
    {
        if (_startTriggered)
            return;
        _startTriggered = true;

        AudioManager.Instance?.PlaySe(SeType.DoorOpen);
        StartCoroutine(Darken(onComplete));
    }

    // 導入：ぼかしを _startBlurAmount から 0 へ補間し、鮮明になったらオーバーレイを無効化する。
    private IEnumerator FocusIn()
    {
        if (_blurMaterial == null || _blurOverlay == null)
            yield break;

        _blurOverlay.enabled = true;

        float elapsed = 0f;
        while (elapsed < _focusInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _focusInDuration);
            _blurMaterial.SetFloat(kBlurSizeId, Mathf.Lerp(_startBlurAmount, 0f, t));
            yield return null;
        }

        // 鮮明になった後は無駄な全画面ブラー描画を避けるため非表示にする。
        _blurMaterial.SetFloat(kBlurSizeId, 0f);
        _blurOverlay.enabled = false;
    }

    // メニュー UI の alpha を 0 → 1 へ補間し、表示しきってから操作可能にする。
    private IEnumerator FadeInMenu()
    {
        if (_menuGroup == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < _menuFadeInDuration)
        {
            elapsed += Time.deltaTime;
            _menuGroup.alpha = Mathf.Clamp01(elapsed / _menuFadeInDuration);
            yield return null;
        }

        _menuGroup.alpha = 1f;
        _menuGroup.interactable = true;
        _menuGroup.blocksRaycasts = true;
    }

    // 開始：黒オーバーレイの alpha を 0 → 1 へ補間し、暗転しきってから完了を通知する。
    private IEnumerator Darken(Action onComplete)
    {
        if (_darkenOverlay != null)
        {
            _darkenOverlay.blocksRaycasts = true; // 暗転中の追加入力を遮断する

            float elapsed = 0f;
            while (elapsed < _darkenDuration)
            {
                elapsed += Time.deltaTime;
                _darkenOverlay.alpha = Mathf.Clamp01(elapsed / _darkenDuration);
                yield return null;
            }
            _darkenOverlay.alpha = 1f;
        }

        onComplete?.Invoke();
    }
}
