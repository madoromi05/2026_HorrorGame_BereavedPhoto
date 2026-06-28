using System.Collections;
using TMPro;
using UnityEngine;

namespace HorrorGame.UI
{
    /// <summary>
    /// インタラクト時のフィードバックメッセージを表示するUIコンポーネント。
    /// Show() を呼ぶとメッセージを表示し、_displayDuration 秒後にフェードアウトする。
    /// </summary>
    public class InteractFeedbackUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Tooltip("メッセージを表示し続ける時間（秒）")]
        [SerializeField] private float _displayDuration = 3f;

        [Tooltip("フェードアウト速度（1秒あたりのアルファ変化量）")]
        [SerializeField] private float _fadeSpeed = 2f;

        private Coroutine _currentRoutine;

        private void Awake()
        {
            DebugCustom.ValidateFields(this,
                (nameof(_messageText), _messageText),
                (nameof(_canvasGroup), _canvasGroup));

            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
        }

        /// メッセージを表示する。表示中に呼ばれた場合はリセットして再表示する。
        public void Show(string message)
        {
            if (_currentRoutine != null)
                StopCoroutine(_currentRoutine);

            _messageText.text = message;
            _currentRoutine = StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            // 即時表示
            _canvasGroup.alpha = 1f;

            yield return new WaitForSeconds(_displayDuration);

            // フェードアウト
            while (_canvasGroup.alpha > 0f)
            {
                _canvasGroup.alpha -= _fadeSpeed * Time.deltaTime;
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            _currentRoutine = null;
        }
    }
}
