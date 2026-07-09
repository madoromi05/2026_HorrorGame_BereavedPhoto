using TMPro;
using UnityEngine;
using HorrorGame.Item;
using UnityEngine.UI;

namespace HorrorGame.UI
{
    /// <summary>
    /// アイテム取得時にアイテム名と説明文をオーバーレイ表示するUIクラス。
    /// Canvas配下のパネルにアタッチし、ItemPickupからShow()を呼ぶ。
    /// </summary>
    public class ItemGetUIPresenter : RevealUIPresenterBase
    {
        [SerializeField] private TextMeshProUGUI _itemNameText;
        [SerializeField] private Image _itemImage;
        [SerializeField] private TextMeshProUGUI _itemDescriptionText;

        private void Awake()
        {
            if (_itemNameText == null)
                DebugCustom.LogError($"[ItemAcquiredUIPresenter] itemNameText が未設定です。", this);
        }

        /// <summary>
        /// アイテムデータを受け取りオーバーレイを表示する。ItemPickupから呼ばれる。
        /// </summary>
        public void Show(ItemData itemData)
        {
            _itemNameText.text = itemData.DisplayName;
            if (_itemDescriptionText != null)
                _itemDescriptionText.text = itemData.Description;
            if (_itemImage != null)
            {
                _itemImage.sprite = itemData.Icon;
                _itemImage.preserveAspect = true;
                _itemImage.enabled = itemData.Icon != null;
            }
            ShowBase();
        }
    }
}