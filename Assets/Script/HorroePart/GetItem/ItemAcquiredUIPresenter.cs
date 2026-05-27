using TMPro;
using UnityEngine;
using HorrorGame.Item;

namespace HorrorGame.UI
{
    /// <summary>
    /// アイテム取得時にアイテム名と説明文をオーバーレイ表示するUIクラス。
    /// Canvas配下のパネルにアタッチし、ItemPickupからShow()を呼ぶ。
    /// </summary>
    public class ItemAcquiredUIPresenter : RevealUIPresenterBase
    {
        [SerializeField] private TextMeshProUGUI itemNameText;
        // [SerializeField] private TextMeshProUGUI itemDescriptionText;

        /// <summary>
        /// アイテムデータを受け取りオーバーレイを表示する。ItemPickupから呼ばれる。
        /// </summary>
        public void Show(ItemData itemData)
        {
            itemNameText.text = itemData.DisplayName;
            // itemDescriptionText.text = itemData.Description;
            ShowBase();
        }
    }
}