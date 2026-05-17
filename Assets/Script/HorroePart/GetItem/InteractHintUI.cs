using TMPro;
using UnityEngine;
using HorrorGame.Player;

namespace HorrorGame.UI
{
    /// <summary>
    /// PlayerInteractor.OnFocusChangedを購読し、
    /// インタラクト可能なオブジェクトに近づいたときに
    /// 操作ヒントをテキストで表示するUIコンポーネント。
    /// Canvas配下のGameObjectにアタッチし、InspectorでPlayerInteractorをアサインすること。
    /// </summary>
    public class InteractHintUI : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor playerInteractor;
        [SerializeField] private TextMeshProUGUI hintText;

        private void Awake()
        {
            // 初期状態は非表示
            SetHintVisible(false);
        }

        private void OnEnable()
        {
            if (playerInteractor == null)
            {
                DebugCustom.LogError("[HintUI] PlayerInteractorがアサインされていません。");
                return;
            }

            playerInteractor.OnFocusChanged += HandleFocusChanged;
        }

        private void OnDisable()
        {
            if (playerInteractor == null) return;
            playerInteractor.OnFocusChanged -= HandleFocusChanged;
        }

        /// <summary>
        /// OnFocusChangedのハンドラ。
        /// hint が null または空のときはフォーカスが外れたと判断して非表示にする。
        /// </summary>
        private void HandleFocusChanged(string hint)
        {
            if (string.IsNullOrEmpty(hint))
            {
                SetHintVisible(false);
                return;
            }

            hintText.text = hint;
            SetHintVisible(true);
        }

        private void SetHintVisible(bool visible)
        {
            hintText.gameObject.SetActive(visible);
        }
    }
}