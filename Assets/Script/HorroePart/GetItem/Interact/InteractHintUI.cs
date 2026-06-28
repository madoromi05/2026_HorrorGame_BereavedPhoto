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
            if (_hintText == null)
                DebugCustom.LogError($"[InteractHintUI] _hintText が未設定です。", this);

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
            if (_suppressed) return;

            if (string.IsNullOrEmpty(hint))
            {
                SetHintVisible(false);
                return;
            }
            DebugCustom.Log($"[HintUI] フォーカスが変わりました。ヒント: {hint}");
            _hintText.text = hint;
            SetHintVisible(true);
        }

        private bool _suppressed = false;

        /// <summary>
        /// UI表示中など、ヒントを強制非表示にしたいときに呼ぶ。
        /// true にすると OnFocusChanged を無視して非表示を維持する。
        /// </summary>
        public void SetSuppressed(bool suppressed)
        {
            _suppressed = suppressed;
            if (suppressed)
                SetHintVisible(false);
        }

        private void SetHintVisible(bool visible)
        {
            _hintText.gameObject.SetActive(visible);
        }
    }
}
