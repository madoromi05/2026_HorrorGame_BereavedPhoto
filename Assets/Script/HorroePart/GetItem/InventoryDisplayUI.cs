using System;
using System.Collections.Generic;
using UnityEngine;
using HorrorGame.Item;

namespace HorrorGame.UI
{
    /// <summary>
    /// プレイヤーの所持アイテムを常時表示するHUD。
    /// アイテムを持っているスロットのみ表示し、0になったら非表示にする。
    /// </summary>
    public class InventoryDisplayUI : MonoBehaviour
    {
        [Serializable]
        private class ItemEntryConfig
        {
            public ItemData itemData;
            [Tooltip("個数を表示しない場合（鍵など）はオフ")]
            public bool showCount = true;
        }

        [SerializeField] private Inventory _playerInventory;
        [SerializeField] private Transform _strandedCont;
        [SerializeField] private ItemStrandedUI _strandedPrefab;
        [SerializeField] private List<ItemEntryConfig> _entries = new();

        private readonly Dictionary<ItemType, (ItemStrandedUI stranded, bool showCount)> _slots = new();

        private void Start()
        {
            if (_playerInventory == null)
            {
                DebugCustom.LogWarning("[InventoryDisplayUI] Inventory が見つかりません。");
                return;
            }

            _playerInventory.OnItemCountChanged += OnCountChanged;
            BuildSlots();
            RefreshAll();
        }

        private void OnDestroy()
        {
            if (_playerInventory != null)
                _playerInventory.OnItemCountChanged -= OnCountChanged;
        }

        private void BuildSlots()
        {
            if (_strandedPrefab == null)
            {
                DebugCustom.LogError("[InventoryDisplayUI] _strandedPrefab が未設定です。", this);
                return;
            }

            foreach (var entry in _entries)
            {
                if (entry.itemData == null) continue;
                var stranded = Instantiate(_strandedPrefab, _strandedCont);
                stranded.Setup(entry.itemData.DisplayName, entry.itemData.Icon);
                stranded.UpdateCount(0, entry.showCount);
                stranded.gameObject.SetActive(false);
                _slots[entry.itemData.ItemType] = (stranded, entry.showCount);
            }
        }

        private void RefreshAll()
        {
            foreach (var (type, slot) in _slots)
            {
                int count = _playerInventory.GetItemCount(type);
                slot.stranded.gameObject.SetActive(count > 0);
                slot.stranded.UpdateCount(count, slot.showCount);
            }
        }

        private void OnCountChanged(ItemType type, int count)
        {
            if (!_slots.TryGetValue(type, out var entry)) return;
            entry.stranded.gameObject.SetActive(count > 0);
            entry.stranded.UpdateCount(count, entry.showCount);
        }
    }
}
