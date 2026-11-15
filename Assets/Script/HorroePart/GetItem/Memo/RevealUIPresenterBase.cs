using UnityEngine;
using UnityEngine.UI;

namespace HorrorGame.UI
{
    /// <summary>
    /// 円形リビールアニメーションで表示・クローズする共通基底クラス。
    /// MemoUIPresenter・ItemAcquiredUIPresenterが継承する。
    /// Show()/Hide()のオーバーライドで各UIの表示内容を設定すること。
    ///
    /// ここが持つのは、オーバーレイの開け閉め（入力・ライト・ヒントの切り替え）と拡張点（ShowBase・Hide・OnHoldComplete）だけ
    /// 長押しの数え方は HoldProgressGauge、円形リビールは CircleRevealAnimator に任せる（委譲）
    /// 共有の Inspector の値は派生クラスのコンポーネントとしてシーンに保存されるため、この基底に置く
    /// </summary>
    public abstract class RevealUIPresenterBase : MonoBehaviour
    {
        [SerializeField] protected GameObject _overlayPanel;
        [SerializeField] protected Image _revealImage;
        [SerializeField] protected InputPlayerController _inputController;
        [SerializeField] protected Image _holdProgressImage;
        [SerializeField] protected GameObject _playerLight;
        [SerializeField] private InteractHintUI _interactHintUI;

        protected bool _isShowing = false;

        private HoldProgressGauge _holdGauge;
        private CircleRevealAnimator _revealAnimator;

        // Awake は派生クラスが使うため、係は初めて使う所で作る
        private HoldProgressGauge HoldGauge => _holdGauge ??= new HoldProgressGauge(_holdProgressImage);
        private CircleRevealAnimator RevealAnimator => _revealAnimator ??= new CircleRevealAnimator(this, _revealImage);

        protected virtual void Start()
        {
            _overlayPanel.SetActive(false);
            HoldGauge.HideGauge();
        }

        private void OnEnable()
        {
            DebugCustom.ValidateFields(this,
                (nameof(_overlayPanel), _overlayPanel),
                (nameof(_revealImage), _revealImage),
                (nameof(_inputController), _inputController),
                (nameof(_holdProgressImage), _holdProgressImage));

            _inputController.OnInteractHeld += OnInteractHeld;
            _inputController.OnInteractReleased += OnInteractReleased;
        }

        private void OnDisable()
        {
            _inputController.OnInteractHeld -= OnInteractHeld;
            _inputController.OnInteractReleased -= OnInteractReleased;
        }

        private void OnDestroy()
        {
            _revealAnimator?.ReleaseMaterial();	// 一度も開いていなければ複製は無い
        }

        private void Update()
        {
            if (!_isShowing) return;

            if (HoldGauge.Tick())
                OnHoldComplete();
        }

        /// <summary>
        /// ホールド完了時に呼ばれる。デフォルトは閉じる。
        /// 派生クラスでオーバーライドすれば、複数ページ送りなど
        /// 別の挙動に差し替えられる（最後は base.OnHoldComplete() で閉じる）。
        /// </summary>
        protected virtual void OnHoldComplete()
        {
            Hide();
        }

        private void OnInteractHeld()
        {
            if (!_isShowing) return;
            HoldGauge.Begin();
        }

        private void OnInteractReleased()
        {
            if (!_isShowing) return;
            HoldGauge.Cancel();
        }

        /// <summary>
        /// オーバーレイ表示して円形リビールアニメーションを再生する。
        /// 派生クラスはこのメソッドをオーバーライドして表示内容をセットした後、
        /// base.ShowBase()を呼ぶこと。
        /// </summary>
        protected void ShowBase()
        {
            _overlayPanel.SetActive(true);
            _isShowing = true;
            _inputController.SetPlayerInputEnabled(false);
            _interactHintUI?.SetSuppressed(true);

            if (_playerLight != null)
                _playerLight.SetActive(false);
            RevealAnimator.Restart();
        }

        public virtual void Hide()
        {
            _overlayPanel.SetActive(false);
            _isShowing = false;
            HoldGauge.Cancel();
            _inputController.SetPlayerInputEnabled(true);
            _interactHintUI?.SetSuppressed(false);
        }
    }
}
