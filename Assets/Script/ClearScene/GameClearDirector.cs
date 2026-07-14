using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// GameClearScene のエンディング演出コントローラー。
/// ドア開閉演出（画像差し替え＋開閉音）の後、エンディング BGM を鳴らし、
/// クレジットを uGUI + コルーチンで下から上へスクロールする。
/// スクロール完了 or スキップでタイトルへ遷移する。
/// </summary>
public class GameClearDirector : MonoBehaviour
{
    [SerializeField] private InputTitleController _input;

    [Header("ドア演出")]
    [SerializeField] private Image _doorImage;
    [Tooltip("閉じたドア（1枚目・3枚目）。")]
    [SerializeField] private Sprite _doorClosed;
    [Tooltip("開いたドア（2枚目）。")]
    [SerializeField] private Sprite _doorOpened;

    [SerializeField] private float _initialShowDuration = 2f;
    [SerializeField] private float _afterDoorOpenWait = 1f;
    [SerializeField] private float _afterDoorCloseWait = 2f;

    [Header("クレジット")]
    [Tooltip("スクロールさせるクレジット全体の RectTransform。")]
    [SerializeField] private RectTransform _creditRoot;
    [Tooltip("スクロール開始 Y（画面下・anchoredPosition.y）。解像度に合わせて調整する。")]
    [SerializeField] private float _scrollStartY = -540f;
    [Tooltip("スクロール終了 Y（流れ切る位置・anchoredPosition.y）。")]
    [SerializeField] private float _scrollEndY = 2000f;
    [SerializeField] private float _scrollSpeed = 100f;

    // スキップ進捗を表示するプログレスバー（Image / Image Type: Filled）
    [SerializeField] private Image _skipProgressImage;
    [SerializeField] private CanvasGroup _skipCanvasGroup;
    [SerializeField] private float _skipHoldDuration = 3f;
    [SerializeField] private float _skipFadeDuration = 0.25f;
    [SerializeField] private float _skipVisibleAlpha = 0.5f;

    private bool  _isHoldingSkip;    // スキップボタンを押している間 true
    private float _skipHoldElapsed;  // 長押し経過秒数
    private bool  _isFinishing;      // タイトル遷移の二重実行防止

    private void Awake()
    {
        DebugCustom.ValidateFields(this,
            (nameof(_input),             _input),
            (nameof(_doorImage),         _doorImage),
            (nameof(_doorClosed),        _doorClosed),
            (nameof(_doorOpened),        _doorOpened),
            (nameof(_creditRoot),        _creditRoot),
            (nameof(_skipProgressImage), _skipProgressImage),
            (nameof(_skipCanvasGroup),   _skipCanvasGroup));
    }

    private void OnEnable()
    {
        if (_input == null) return;
        _input.OnMenuHeld     += BeginSkipHold;
        _input.OnMenuReleased += EndSkipHold;
    }

    private void OnDisable()
    {
        if (_input == null) return;
        _input.OnMenuHeld     -= BeginSkipHold;
        _input.OnMenuReleased -= EndSkipHold;
    }

    private void Start()
    {
        LockCursor();

        // スキップ用プログレスバーは初期状態で非表示（押している間だけフェードインさせる）。
        _skipCanvasGroup.alpha = 0f;
        _skipProgressImage.fillAmount = 0f;

        _doorImage.sprite = _doorClosed;
        StartCoroutine(EndingSequence());
    }

    // エンディング中はカーソルを固定し続け、スキップボタンの長押しを毎フレーム進める。
    private void Update()
    {
        KeepCursorLocked();
        UpdateSkipHold();
    }

    // このシーンでは Start() の1回設定だと毎フレーム表示側に戻されてしまうため、
    // 状態が崩れたフレームだけカーソル固定を再適用する（既に固定済みなら何もしない）。
    private void KeepCursorLocked()
    {
        if (!Cursor.visible && Cursor.lockState == CursorLockMode.Locked) return;
        LockCursor();
    }

    // 押している間だけプログレスバーをフェードインさせながら進め、
    // 規定秒数まで押し続けたらクリア画面をスキップしてタイトルへ遷移する。
    // 途中で離したら経過とバーをリセットし、フェードアウトさせる。
    private void UpdateSkipHold()
    {
        float fadeTarget = _isHoldingSkip ? _skipVisibleAlpha : 0f;
        float fadeStep   = _skipFadeDuration > 0f ? Time.deltaTime / _skipFadeDuration : 1f;
        _skipCanvasGroup.alpha = Mathf.MoveTowards(_skipCanvasGroup.alpha, fadeTarget, fadeStep);

        if (!_isHoldingSkip)
        {
            _skipHoldElapsed = 0f;
            _skipProgressImage.fillAmount = 0f;
            return;
        }

        _skipHoldElapsed += Time.deltaTime;
        _skipProgressImage.fillAmount = _skipHoldElapsed / _skipHoldDuration;

        if (_skipHoldElapsed >= _skipHoldDuration)
            GoToTitle();
    }

    // 本編と同様、マウスを隠し位置基準を中心に固定する。
    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    // ドア開閉演出 → BGM → クレジットスクロール → タイトル遷移。
    private IEnumerator EndingSequence()
    {
        // 1枚目
        yield return new WaitForSeconds(_initialShowDuration);

        // 2枚目
        _doorImage.sprite = _doorOpened;
        yield return PlaySeAndWait(SeType.DoorOpen, _afterDoorOpenWait);

        // 3枚目
        _doorImage.sprite = _doorClosed;
        yield return PlaySeAndWait(SeType.DoorClose, _afterDoorCloseWait);

        _doorImage.gameObject.SetActive(false);
        // エンディング BGM を鳴らし、クレジットをスクロールする。
        AudioManager.Instance.PlayBgm(BgmType.Ending);
        yield return ScrollCredit();

        GoToTitle();
    }

    // SE を鳴らし、そのクリップ長＋余韻ぶん待機する。クリップ未設定ならクリップ長は 0 扱い。
    private IEnumerator PlaySeAndWait(SeType type, float extraWait)
    {
        AudioManager.Instance.PlaySe(type);
        var clip = AudioManager.Instance.GetSeClip(type);
        float clipLength = clip != null ? clip.length : 0f;
        yield return new WaitForSeconds(clipLength + extraWait);
    }

    // クレジットを下から上へスクロールする。終了 Y 到達で終わる（スキップは長押しで別途処理）。
    private IEnumerator ScrollCredit()
    {
        Vector2 pos = _creditRoot.anchoredPosition;
        pos.y = _scrollStartY;
        _creditRoot.anchoredPosition = pos;

        while (pos.y < _scrollEndY)
        {
            pos.y += _scrollSpeed * Time.deltaTime;
            _creditRoot.anchoredPosition = pos;
            yield return null;
        }
    }

    // スキップ長押しの開始／終了。押している間だけ Update() でプログレスを進める。
    private void BeginSkipHold() => _isHoldingSkip = true;
    private void EndSkipHold()   => _isHoldingSkip = false;

    // タイトルへ遷移する（二重実行防止）。
    private void GoToTitle()
    {
        if (_isFinishing) return;
        _isFinishing = true;
        SceneManager.LoadScene(GameProgressManager.SceneTitleName);
    }
}
