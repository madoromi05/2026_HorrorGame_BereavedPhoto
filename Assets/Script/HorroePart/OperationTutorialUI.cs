using UnityEngine;

namespace HorrorGame.UI
{
    /// <summary>
    /// OperationCanvas にアタッチする。
    /// </summary>
    public class OperationTutorialUI : MonoBehaviour
    {
        private static OperationTutorialUI _instance;

        private void Awake() => _instance = this;
        private void OnDestroy() { if (_instance == this) _instance = null; }

        /// <summary>操作説明UIが有効なら非表示にする。存在しなければ何もしない。</summary>
        public static void HideIfActive()
        {
            if (_instance != null && _instance.gameObject.activeSelf)
                _instance.gameObject.SetActive(false);
        }
    }
}
