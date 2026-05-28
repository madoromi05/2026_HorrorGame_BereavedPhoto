using HorrorGame.Interaction;
using HorrorGame.UI;
using UnityEngine;

namespace HorrorGame.Item
{
    /// <summary>
    /// ワールドに配置されたアイテムオブジェクト。
    /// IInteractableを実装し、インタラクトするとInventoryへアイテムを追加する。
    /// 取得後は自分を非活性化してワールドから消える。
    /// Inspectorでitemをnullにすると何も起きないため必ず設定すること。
    /// </summary>
    [AddComponentMenu("HorrorGame/Item/ItemPickup")]
    [RequireComponent(typeof(Collider))]
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemData _item;

        public bool CanInteract => _item != null;
        public string HintText => _item != null ? $"[E] {_item.DisplayName}を取得" : string.Empty;

        private ItemAcquiredUIPresenter _uiPresenter;

        public void Init(ItemAcquiredUIPresenter presenter)
        {
            _uiPresenter = presenter;
        }

        /// <summary>
        /// プレイヤーのInventoryにアイテムを追加し、そのGameObjectを非活性化する。
        /// InventoryはGetComponentInParentは使わず、PlayerInteractor経由で
        /// 渡された参照を使う設計のため、呼び出し元がInventoryを保持していること。
        /// </summary>
        public void OnInteract()
        {
            if (!CanInteract) return;

            var inventory = FindFirstObjectByType<Inventory>();
            if (inventory == null)
            {
                DebugCustom.LogWarning("[ItemPickup] Inventoryが見つかりません。");
                return;
            }

            inventory.AddItem(_item);
            _uiPresenter?.Show(_item);
            gameObject.SetActive(false);
        }
    }
}
