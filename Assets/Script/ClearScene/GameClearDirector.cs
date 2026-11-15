using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GameClearScene のエンディング演出コントローラー
/// ドアが開く演出（画像差し替え＋開閉音）の後、画面を白く塗り潰して場面を転換し、
/// エンディング BGM を鳴らしながらクレジットを uGUI + コルーチンで下から上へスクロールする
/// スクロール完了 or スキップでタイトルへ遷移する
///
/// 白は「終着点」ではなく「通過点」として扱う。白で覆っている間にドアを捨て、
/// 白が引くとクレジット背景が現れる、という目隠しの役割を持たせている
/// （ドアからクレジットへの切り替わりを直接見せないため）
///
/// 窓口（Facade）。調整値はここの Inspector に置き、段（IEndingStep）は演出の始まりに作って渡し、毎フレーム動く係には使うたびに渡す
/// 段を順に流すこと・白の裏で舞台を差し替えること・タイトルへ移る判断・カーソル固定だけをここで行う
/// </summary>
public class GameClearDirector : MonoBehaviour
{
    [SerializeField] private InputTitleController _input;

    [Header("ドア演出")]
    [SerializeField] private Image _doorImage;
    [SerializeField] private Sprite _doorClosed;
    [SerializeField] private Sprite _doorOpened;

    [SerializeField] private float _initialShowDuration = 2f;
    [SerializeField] private float _afterDoorOpenWait = 1f;

    [Header("白転演出")]
    [SerializeField] private CanvasGroup _whiteOverlay;
    [Tooltip("開いたドアが白く塗り潰されるまでの時間（秒）。")]
    [SerializeField] private float _whitenDuration = 2f;
    [Tooltip("白く覆いきった状態を保持する余韻（秒）。")]
    [SerializeField] private float _holdWhiteDuration = 2f;
    [Tooltip("白が引いてクレジット背景が現れるまでの時間（秒）。")]
    [SerializeField] private float _whiteFadeOutDuration = 1.5f;

    [Header("タイトル暗転")]
    [Tooltip("タイトルへ戻る前に全画面を黒く覆う CanvasGroup（黒 Image・alpha 0 開始）。")]
    [SerializeField] private CanvasGroup _darkenOverlay;
    [Tooltip("黒く覆いきってタイトルへ遷移するまでの時間（秒）。")]
    [SerializeField] private float _darkenDuration = 1.5f;

    [Header("クレジット")]
    [Tooltip("スクロールさせるクレジット全体の RectTransform。")]
    [SerializeField] private RectTransform _creditRoot;
    [Tooltip("スクロール開始 Y（画面下・anchoredPosition.y）。解像度に合わせて調整する。")]
    [SerializeField] private float _scrollStartY = -540f;
    [Tooltip("スクロール終了 Y（流れ切る位置・anchoredPosition.y）。")]
    [SerializeField] private float _scrollEndY = 2000f;
    [SerializeField] private float _scrollSpeed = 100f;

    [Header("クレジットスキップ")]
    // スキップ進捗を表示するプログレスバー（Image / Image Type: Filled）
    [SerializeField] private Image _skipProgressImage;
    [SerializeField] private CanvasGroup _skipCanvasGroup;
    [SerializeField] private float _skipHoldDuration = 3f;
    [SerializeField] private float _skipFadeDuration = 0.25f;
    [SerializeField] private float _skipVisibleAlpha = 0.5f;

    private bool  _isFinishing;      // タイトル遷移の二重実行防止

    private DoorOpenStep        _doorOpen;
    private WhiteTransitionStep _whiteTransition;
    private CreditScrollStep    _creditScroll;
    private CreditSkipHold      _skipHold;
    private TitleReturnFade     _titleReturn;

    private void Awake()
    {
        DebugCustom.ValidateFields(this,
            (nameof(_input),             _input),
            (nameof(_doorImage),         _doorImage),
            (nameof(_doorClosed),        _doorClosed),
            (nameof(_doorOpened),        _doorOpened),
            (nameof(_whiteOverlay),      _whiteOverlay),
            (nameof(_darkenOverlay),     _darkenOverlay),
            (nameof(_creditRoot),        _creditRoot),
            (nameof(_skipProgressImage), _skipProgressImage),
            (nameof(_skipCanvasGroup),   _skipCanvasGroup));

        _skipHold    = new CreditSkipHold(_skipProgressImage, _skipCanvasGroup);	// 入力の購読に要るので OnEnable より前に作る
        _titleReturn = new TitleReturnFade(_darkenOverlay);
    }

    private void OnEnable()
    {
        if (_input == null) return;
        _input.OnMenuHeld     += _skipHold.Begin;
        _input.OnMenuReleased += _skipHold.End;
    }

    private void OnDisable()
    {
        if (_input == null) return;
        _input.OnMenuHeld     -= _skipHold.Begin;
        _input.OnMenuReleased -= _skipHold.End;
    }

    private void Start()
    {
        // 前シーンの BGM が DontDestroyOnLoad の AudioManager 経由で鳴り続けるため、
        // ClearScene 開始時に即停止する。エンディング BGM は EndingSequence 内で改めて鳴らす
        AudioManager.Instance?.StopBgm(0f);

        GameCursor.Lock();

        BuildSteps();
        _skipHold.HideAtStart();
        _whiteTransition.HideAtStart();
        _titleReturn.HideAtStart();

        _doorOpen.ShowClosed();
        _creditScroll.HideUntilStart();
        StartCoroutine(EndingSequence());
    }

    // エンディング中はカーソルを固定し続け、スキップボタンの長押しを毎フレーム進める
    private void Update()
    {
        KeepCursorLocked();
        if (_skipHold.Tick(_skipHoldDuration, _skipFadeDuration, _skipVisibleAlpha))
            GoToTitle();
    }

    // このシーンでは Start() の1回設定だと毎フレーム表示側に戻されてしまうため、
    // 状態が崩れたフレームだけカーソル固定を再適用する（既に固定済みなら何もしない）
    private void KeepCursorLocked()
    {
        if (GameCursor.IsLocked) return;
        GameCursor.Lock();
    }

    // 演出の段を作る。調整値は演出が始まるときに読む
    private void BuildSteps()
    {
        _doorOpen        = new DoorOpenStep(_doorImage, _doorClosed, _doorOpened, _initialShowDuration, _afterDoorOpenWait);
        _whiteTransition = new WhiteTransitionStep(_whiteOverlay, SeType.DoorClose,	// ドアが閉まる音は画面外の出来事として白転と重ねて鳴らす
            _whitenDuration, _holdWhiteDuration, _whiteFadeOutDuration, SwapStageBehindWhite);
        _creditScroll    = new CreditScrollStep(_creditRoot, _scrollStartY, _scrollEndY, _scrollSpeed);
    }

    // ドアが開く演出 → 白転 → BGM → クレジットスクロール → タイトル遷移
    private IEnumerator EndingSequence()
    {
        foreach (var step in new IEndingStep[] { _doorOpen, _whiteTransition, _creditScroll })	// 演出の順番はここだけで決まる
            yield return step.Play();

        GoToTitle();
    }

    // 白で覆われている間に舞台を整える（この後に白が引く）。位置決めを白の裏で済ませないと、
    // 白が引く途中でクレジットが画面中央に居座ったまま見えてしまう
    private void SwapStageBehindWhite()
    {
        _doorOpen.Hide();
        _creditScroll.MoveToStart();
        AudioManager.Instance.PlayBgm(BgmType.Ending);
    }

    // タイトルへ遷移する（二重実行防止）。暗転しきってからロードする
    private void GoToTitle()
    {
        if (_isFinishing) return;
        _isFinishing = true;
        StartCoroutine(_titleReturn.Play(_darkenDuration));
    }
}
