using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HorrorGame.UI
{
    /// <summary>
    /// 円形リビール演出と長押しクローズを共通化した抽象基底クラス。
    /// MemoUIPresenter・ItemAcquiredUIPresenterが継承する。
    /// Show()/Hide()のオーバーライドで各UIの表示内容を実装すること。
    /// </summary>
    public abstract class RevealUIPresenterBase : MonoBehaviour
    {
        [SerializeField] protected GameObject overlayPanel;
        [SerializeField] protected Image revealImage;
        [SerializeField] protected InputPlayerController inputController;
        [SerializeField] protected Image holdProgressImage;
        [SerializeField] protected GameObject playerLight;

        protected bool isShowing = false;

        private float holdElapsed = 0f;
        private bool isHolding = false;
        private Coroutine revealCoroutine;
        private Material revealMaterialInstance;

        private const float kHoldDuration = 1f;
        private const float kRevealDuration = 0.6f;
        private readonly int kShaderRadius = Shader.PropertyToID("_Radius");

        protected virtual void Start()
        {
            overlayPanel.SetActive(false);
            holdProgressImage.fillAmount = 0f;
            holdProgressImage.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            inputController.OnInteractHeld += OnInteractHeld;
            inputController.OnInteractReleased += OnInteractReleased;
        }

        private void OnDisable()
        {
            inputController.OnInteractHeld -= OnInteractHeld;
            inputController.OnInteractReleased -= OnInteractReleased;
        }

        private void Update()
        {
            if (!isShowing || !isHolding) return;

            holdElapsed += Time.deltaTime;
            holdProgressImage.fillAmount = holdElapsed / kHoldDuration;

            if (holdElapsed >= kHoldDuration)
                Hide();
        }

        private void OnInteractHeld()
        {
            if (!isShowing) return;
            isHolding = true;
            holdElapsed = 0f;
            holdProgressImage.gameObject.SetActive(true);
        }

        private void OnInteractReleased()
        {
            if (!isShowing) return;
            isHolding = false;
            holdElapsed = 0f;
            holdProgressImage.fillAmount = 0f;
            holdProgressImage.gameObject.SetActive(false);
        }

        /// <summary>
        /// オーバーレイを表示し円形リビール演出を再生する。
        /// 派生クラスはこのメソッドをオーバーライドして表示内容をセットした後、
        /// base.ShowBase()を呼ぶこと。
        /// </summary>
        protected void ShowBase()
        {
            overlayPanel.SetActive(true);
            isShowing = true;
            inputController.SetPlayerInputEnabled(false);

            if (playerLight != null)
                playerLight.SetActive(false);
            if (revealCoroutine != null)
                StopCoroutine(revealCoroutine);
            revealCoroutine = StartCoroutine(PlayRevealAnimation());
        }

        public virtual void Hide()
        {
            overlayPanel.SetActive(false);
            isShowing = false;
            isHolding = false;
            holdElapsed = 0f;
            holdProgressImage.fillAmount = 0f;
            holdProgressImage.gameObject.SetActive(false);
            inputController.SetPlayerInputEnabled(true);
        }

        /// <summary>
        /// _Radiusを0→1.5にアニメーションさせて円形リビールを再生する。
        /// 1.5まで広げるのはUV対角線（√0.5≒0.707）を超えて画面端まで確実に表示するため。
        /// </summary>
        private IEnumerator PlayRevealAnimation()
        {
            DebugCustom.Log("PlayRevealAnimation started");
            revealMaterialInstance = revealImage.material;
            revealMaterialInstance.SetFloat(kShaderRadius, 0f);

            float elapsed = 0f;
            while (elapsed < kRevealDuration)
            {
                elapsed += Time.deltaTime;
                // easeOutCubicで最初は速く、終盤は滑らかに広がる
                float t = elapsed / kRevealDuration;
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                revealMaterialInstance.SetFloat(kShaderRadius, Mathf.Lerp(0f, 1.5f, eased));
                yield return null;
            }

            revealMaterialInstance.SetFloat(kShaderRadius, 1.5f);
            revealCoroutine = null;
        }
    }
}