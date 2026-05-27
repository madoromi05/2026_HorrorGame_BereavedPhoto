using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorrorGame.UI
{
    /// <summary>
    /// メモの本文を画面中央にオーバーレイ表示するUI制御クラス。
    /// Canvas配下のパネルにアタッチし、MemoItem から Show() を呼ぶ。
    /// Eキーまたは閉じるボタンで非表示になる。
    /// </summary>
    public class MemoUIPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject overlayPanel;
        [SerializeField] private TextMeshProUGUI memoText;
        [SerializeField] private InputPlayerController inputController;
        [SerializeField] private Image holdProgressImage;

        private bool isShowing = false;
        private float holdElapsed = 0f;
        private bool isHolding = false;
        private const float kHoldDuration = 1f;

        private void Start()
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


        private void OnInteractClose()
        {
            if (isShowing) Hide();
        }

        /// <summary>
        /// メモ本文を受け取りオーバーレイを表示する。MemoItem から呼ばれる。
        /// </summary>
        public void Show(string content)
        {
            memoText.text = content;
            overlayPanel.SetActive(true);
            isShowing = true;
            inputController.SetPlayerInputEnabled(false);
        }

        public void Hide()
        {
            overlayPanel.SetActive(false);
            isShowing = false;
            isHolding = false;
            holdElapsed = 0f;
            holdProgressImage.fillAmount = 0f;
            holdProgressImage.gameObject.SetActive(false);
            inputController.SetPlayerInputEnabled(true);
        }

    }
}