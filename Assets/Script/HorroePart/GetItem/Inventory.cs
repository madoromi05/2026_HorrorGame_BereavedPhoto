using System;
using System.Collections.Generic;
using UnityEngine;

namespace HorrorGame.Item
{
    /// <summary>
    /// プレイヤーが取得したアイテムを管理するコンポーネント。
    /// 現在はItemTypeの所持フラグのみを扱う。
    /// 将来的にスタック数・重量・スロット制限を追加する場合は
    /// InventorySlot構造体を導入してこのクラスを拡張すること。
    /// PlayerInteractorと同じGameObjectにアタッチして使用する。
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        /// <summary>アイテム取得時に発火。UIやドア解錠チェックで購読する。</summary>
        public event Action<ItemData> OnItemAdded;

        private readonly HashSet<ItemType> acquiredItems = new();

        /// <summary>アイテムを所持リストに追加し、OnItemAddedを発火する。</summary>
        public void AddItem(ItemData itemData)
        {
            if (itemData == null)
            {
                Debug.LogWarning("[Inventory] nullのItemDataを追加しようとしました。");
                return;
            }

            acquiredItems.Add(itemData.ItemType);
            OnItemAdded?.Invoke(itemData);

            Debug.Log($"[Inventory] 取得: {itemData.DisplayName}");
        }

        /// <summary>指定種別のアイテムを所持しているか。ドア解錠などの判定に使う。</summary>
        public bool HasItem(ItemType type) => acquiredItems.Contains(type);

        /// <summary>デバッグ用：所持中のアイテム一覧をログ出力する。</summary>
        [ContextMenu("Log Inventory")]
        private void LogInventory()
        {
            Debug.Log($"[Inventory] 所持アイテム数: {acquiredItems.Count}");
            foreach (var item in acquiredItems)
            {
                Debug.Log($"  - {item}");
            }
        }
    }
}