using System;
using System.Collections.Generic;
using UnityEngine;

namespace HorrorGame.Item
{
    /// <summary>
    /// プレイヤーが獲得したアイテムを管理するコンポーネント。
    /// PlayerInteractorと同じGameObjectにアタッチして使用する。
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        /// アイテム取得時に発火。UIやドア判定で購読する。
        public event Action<ItemData> OnItemAdded;

        private readonly HashSet<ItemType> _acquiredItems = new();

        /// アイテムを取得済みリストに追加し、OnItemAddedを発火する。
        public void AddItem(ItemData itemData)
        {
            if (itemData == null)
            {
                DebugCustom.LogWarning("[Inventory] nullのItemDataを追加しようとしました。");
                return;
            }

            _acquiredItems.Add(itemData.ItemType);
            OnItemAdded?.Invoke(itemData);

            DebugCustom.Log($"[Inventory] 取得: {itemData.DisplayName}");
        }

        /// 指定種類のアイテムを取得しているか。ドア判定などに使う。
        public bool HasItem(ItemType type) => _acquiredItems.Contains(type);

        /// <summary>デバッグ: アイテムタイプを直接追加（UIイベントなし）。</summary>
        public void DebugAddItem(ItemType type) => _acquiredItems.Add(type);
    }
}
