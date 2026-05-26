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
        [SerializeField] private Button closeButton;
        [SerializeField] private InputPlayerController inputController;

        private bool isShowing = false;

        private void OnEnable()
        {
            inputController.OnInteractPerformed += OnInteractClose;
        }

        private void OnDisable()
        {
            inputController.OnInteractPerformed -= OnInteractClose;
        }

        private void Start()
        {
            overlayPanel.SetActive(false);
            closeButton.onClick.AddListener(Hide);
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
        }

        public void Hide()
        {
            overlayPanel.SetActive(false);
            isShowing = false;
        }
    }
}