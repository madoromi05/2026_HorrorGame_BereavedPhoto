using System;
using System.Collections.Generic;
using UnityEngine;

namespace HorrorGame.Item
{
    /// <summary>
    /// プレイヤーが取得したアイテムを管理するコンポーネント。
    /// PlayerInteractorと同じGameObjectにアタッチして使用する。
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        /// アイテム取得時に発火。UIやドア解錠チェックで購読する。
        public event Action<ItemData> OnItemAdded;

        private readonly HashSet<ItemType> acquiredItems = new();

        /// アイテムを所持リストに追加し、OnItemAddedを発火する。
        public void AddItem(ItemData itemData)
        {
            if (itemData == null)
            {
                DebugCustom.LogWarning("[Inventory] nullのItemDataを追加しようとしました。");
                return;
            }

            acquiredItems.Add(itemData.ItemType);
            OnItemAdded?.Invoke(itemData);

            DebugCustom.Log($"[Inventory] 取得: {itemData.DisplayName}");
        }

        /// 指定種別のアイテムを所持しているか。ドア解錠などの判定に使う。
        public bool HasItem(ItemType type) => acquiredItems.Contains(type);
    }
}