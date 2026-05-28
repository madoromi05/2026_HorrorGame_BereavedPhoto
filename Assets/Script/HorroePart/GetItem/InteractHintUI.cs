using TMPro;
using UnityEngine;
using HorrorGame.Player;

namespace HorrorGame.UI
{
    /// <summary>
    /// PlayerInteractor.OnFocusChangedを購読し、
    /// インタラクト可能なオブジェクトに近づいたときに
    /// そのヒントをテキストで表示するUIコンポーネント。
    /// Canvasの下のGameObjectにアタッチし、InspectorでPlayerInteractorをアサインすること。
    /// </summary>
    public class InteractHintUI : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor _playerInteractor;
        [SerializeField] private TextMeshProUGUI _hintText;

        private void Awake()
        {
            SetHintVisible(false);
        }

        private void OnEnable()
        {
            if (_playerInteractor == null)
            {
                DebugCustom.LogError("[HintUI] PlayerInteractorがアサインされていません。");
                return;
            }
            _playerInteractor.OnFocusChanged += HandleFocusChanged;
        }

        private void OnDisable()
        {
            if (_playerInteractor == null) return;
            _playerInteractor.OnFocusChanged -= HandleFocusChanged;
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
            DebugCustom.Log($"[HintUI] フォーカスが変わりました。ヒント: {hint}");
            _hintText.text = hint;
            SetHintVisible(true);
        }

        private void SetHintVisible(bool visible)
        {
            _hintText.gameObject.SetActive(visible);
        }
    }
}
