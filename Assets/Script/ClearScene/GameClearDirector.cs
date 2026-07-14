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
    [Header("入力")]
    [Tooltip("スキップ入力の受け口。未割当ならシーンから取得する。")]
    [SerializeField] private InputTitleController _input;

    [Header("ドア演出")]
    [Tooltip("ドア画像を表示する Image。")]
    [SerializeField] private Image _doorImage;
    [Tooltip("閉じたドア（1枚目・3枚目）。")]
    [SerializeField] private Sprite _doorClosed;
    [Tooltip("開いたドア（2枚目）。")]
    [SerializeField] private Sprite _doorOpened;

    [Tooltip("最初に閉じたドアを見せる秒数。")]
    [SerializeField] private float _initialShowDuration = 2f;
    [Tooltip("ドア開き音を鳴らし終えた後の追加待機秒数。")]
    [SerializeField] private float _afterDoorOpenWait = 1f;
    [Tooltip("ドア閉じ音を鳴らし終えた後の追加待機秒数。")]
    [SerializeField] private float _afterDoorCloseWait = 2f;

    [Header("クレジット")]
    [Tooltip("スクロールさせるクレジット全体の RectTransform。")]
    [SerializeField] private RectTransform _creditRoot;
    [Tooltip("スクロール開始 Y（画面下・anchoredPosition.y）。解像度に合わせて調整する。")]
    [SerializeField] private float _scrollStartY = -540f;
    [Tooltip("スクロール終了 Y（流れ切る位置・anchoredPosition.y）。")]
    [SerializeField] private float _scrollEndY = 2000f;
    [Tooltip("スクロール速度（px/秒）。")]
    [SerializeField] private float _scrollSpeed = 100f;

    private bool _isScrolling;    // クレジットスクロール中のみスキップを受け付ける
    private bool _skipRequested;  // スキップ要求フラグ
    private bool _isFinishing;    // タイトル遷移の二重実行防止

    private void Awake()
    {
        DebugCustom.ValidateFields(this,
            (nameof(_input),      _input),
            (nameof(_doorImage),  _doorImage),
            (nameof(_doorClosed), _doorClosed),
            (nameof(_doorOpened), _doorOpened),
            (nameof(_creditRoot), _creditRoot));
    }

    private void OnEnable()
    {
        if (_input != null) _input.OnMenuPerformed += SkipCredit;
    }

    private void OnDisable()
    {
        if (_input != null) _input.OnMenuPerformed -= SkipCredit;
    }

    private void Start()
    {
        LockCursor();

        _doorImage.sprite = _doorClosed;
        StartCoroutine(EndingSequence());
    }

    // エンディング中はカーソルを固定し続ける。
    // このシーンでは Start() の1回設定だと毎フレーム表示側に戻されてしまうため、
    // 状態が崩れたフレームだけ再適用する（既に固定済みなら何もしない）。
    private void Update()
    {
        if (!Cursor.visible && Cursor.lockState == CursorLockMode.Locked) return;
        LockCursor();
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

    // クレジットを下から上へスクロールする。終了 Y 到達 or スキップで終わる。
    private IEnumerator ScrollCredit()
    {
        Vector2 pos = _creditRoot.anchoredPosition;
        pos.y = _scrollStartY;
        _creditRoot.anchoredPosition = pos;

        _isScrolling   = true;
        _skipRequested = false;

        while (pos.y < _scrollEndY && !_skipRequested)
        {
            pos.y += _scrollSpeed * Time.deltaTime;
            _creditRoot.anchoredPosition = pos;
            yield return null;
        }

        _isScrolling = false;
    }

    // クリック時、クレジットスクロール中のみスキップ要求を立てる（ドア演出中は無視）。
    private void SkipCredit()
    {
        if (_isScrolling) _skipRequested = true;
    }

    // タイトルへ遷移する（二重実行防止）。
    private void GoToTitle()
    {
        if (_isFinishing) return;
        _isFinishing = true;
        SceneManager.LoadScene(GameProgressManager.SceneTitleName);
    }
}
