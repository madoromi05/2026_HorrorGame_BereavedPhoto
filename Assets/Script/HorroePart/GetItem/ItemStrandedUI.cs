using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorrorGame.UI
{
    /// <summary>
    /// インベントリパネル内の1アイテム枠。
    /// Prefabにアタッチして InventoryDisplayUI から生成する。
    /// </summary>
    public class ItemStrandedUI : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _countText;
        [SerializeField] private GameObject _countRoot; // カウントが不要な場合に非表示

        public void Setup(string displayName, Sprite icon)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.preserveAspect = true;
                _iconImage.enabled = icon != null;
            }
        }

        /// <param name="count">0 = 所持なし（呼び出し側で非表示にすること）</param>
        /// <param name="showCount">false のとき個数表示を隠す（鍵など）</param>
        public void UpdateCount(int count, bool showCount = true)
        {
            if (_countRoot != null)
                _countRoot.SetActive(showCount);

            if (_countText != null && showCount)
                _countText.text = count.ToString();
        }
    }
}
