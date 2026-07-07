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

        /// アイテム個数変化時に発火。HUDの更新に使用する。
        public event Action<ItemType, int> OnItemCountChanged;

        private readonly Dictionary<ItemType, int> _acquiredItems = new();

        /// アイテムを取得済みリストに追加し、各イベントを発火する。
        public void AddItem(ItemData itemData)
        {
            if (itemData == null)
            {
                DebugCustom.LogWarning("[Inventory] nullのItemDataを追加しようとしました。");
                return;
            }

            _acquiredItems.TryGetValue(itemData.ItemType, out int current);
            _acquiredItems[itemData.ItemType] = current + 1;
            OnItemAdded?.Invoke(itemData);
            OnItemCountChanged?.Invoke(itemData.ItemType, _acquiredItems[itemData.ItemType]);

            DebugCustom.Log($"[Inventory] 取得: {itemData.DisplayName}");
        }

        /// 指定アイテムを無音で初期付与する。ゲーム開始時の初期アイテム設定に使用する。
        public void InitializeItems(ItemType type, int count)
        {
            if (count <= 0) return;
            _acquiredItems.TryGetValue(type, out int current);
            _acquiredItems[type] = current + count;
            OnItemCountChanged?.Invoke(type, _acquiredItems[type]);
        }

        /// アイテムを1個消費する。消費できた場合 true を返す。
        public bool ConsumeItem(ItemType type)
        {
            if (!HasItem(type)) return false;
            _acquiredItems[type]--;
            OnItemCountChanged?.Invoke(type, _acquiredItems[type]);
            return true;
        }

        /// 指定種類のアイテムを1個以上取得しているか。ドア判定などに使う。
        public bool HasItem(ItemType type) =>
            _acquiredItems.TryGetValue(type, out int c) && c > 0;

        /// 指定種類のアイテム所持数を返す。
        public int GetItemCount(ItemType type) =>
            _acquiredItems.TryGetValue(type, out int c) ? c : 0;

        /// <summary>デバッグ: アイテムタイプを直接追加（UIイベントなし）。</summary>
        public void DebugAddItem(ItemType type)
        {
            _acquiredItems.TryGetValue(type, out int current);
            _acquiredItems[type] = current + 1;
        }
    }
}
