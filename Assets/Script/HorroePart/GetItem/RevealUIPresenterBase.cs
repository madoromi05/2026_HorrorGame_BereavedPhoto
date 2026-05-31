using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HorrorGame.UI
{
    /// <summary>
    /// 円形リビールアニメーションで表示・クローズする共通基底クラス。
    /// MemoUIPresenter・ItemAcquiredUIPresenterが継承する。
    /// Show()/Hide()のオーバーライドで各UIの表示内容を設定すること。
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

        private float _holdElapsed = 0f;
        private bool _isHolding = false;
        private Coroutine _revealCoroutine;
        private Material _revealMaterialInstance;

        private const float kHoldDuration = 1f;
        private const float kRevealDuration = 0.6f;
        private readonly int kShaderRadius = Shader.PropertyToID("_Radius");

        protected virtual void Start()
        {
            _overlayPanel.SetActive(false);
            _holdProgressImage.fillAmount = 0f;
            _holdProgressImage.gameObject.SetActive(false);
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

        private void Update()
        {
            if (!_isShowing || !_isHolding) return;

            _holdElapsed += Time.deltaTime;
            _holdProgressImage.fillAmount = _holdElapsed / kHoldDuration;

            if (_holdElapsed >= kHoldDuration)
                Hide();
        }

        private void OnInteractHeld()
        {
            if (!_isShowing) return;
            _isHolding = true;
            _holdElapsed = 0f;
            _holdProgressImage.gameObject.SetActive(true);
        }

        private void OnInteractReleased()
        {
            if (!_isShowing) return;
            _isHolding = false;
            _holdElapsed = 0f;
            _holdProgressImage.fillAmount = 0f;
            _holdProgressImage.gameObject.SetActive(false);
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
            if (_revealCoroutine != null)
                StopCoroutine(_revealCoroutine);
            _revealCoroutine = StartCoroutine(PlayRevealAnimation());
        }

        public virtual void Hide()
        {
            _overlayPanel.SetActive(false);
            _isShowing = false;
            _isHolding = false;
            _holdElapsed = 0f;
            _holdProgressImage.fillAmount = 0f;
            _holdProgressImage.gameObject.SetActive(false);
            _inputController.SetPlayerInputEnabled(true);
            _interactHintUI?.SetSuppressed(false);
        }

        /// <summary>
        /// _Radiusを0から1.5にアニメーションして円形リビールを再生する。
        /// 1.5まで広げるのはUV対角距離（最大0.5→0.707）を超えて画面端まで確実に表示するため。
        /// </summary>
        private IEnumerator PlayRevealAnimation()
        {
            _revealMaterialInstance = _revealImage.material;
            _revealMaterialInstance.SetFloat(kShaderRadius, 0f);

            float elapsed = 0f;
            while (elapsed < kRevealDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / kRevealDuration;
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                _revealMaterialInstance.SetFloat(kShaderRadius, Mathf.Lerp(0f, 1.5f, eased));
                yield return null;
            }

            _revealMaterialInstance.SetFloat(kShaderRadius, 1.5f);
            _revealCoroutine = null;
        }
    }
}
