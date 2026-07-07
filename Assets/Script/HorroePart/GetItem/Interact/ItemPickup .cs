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
        [SerializeField] private int _obstructionRespawnMinTiles = 8;
        public bool CanInteract => _item != null;
        public string HintText => _item != null ? $"[E] {_item.DisplayName}を取得" : string.Empty;

        private ItemAcquiredUIPresenter _uiPresenter;
        private ObstructionRespawner _respawner;
        public void Init(ItemAcquiredUIPresenter presenter, ObstructionRespawner respawner = null)
        {
            _uiPresenter = presenter;
            _respawner = respawner;
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

            // 妨害アイテムは初回取得時のみ名前・説明を表示し、2回目以降は表示しない。
            // AddItemで所持数が増える前に取得済みかどうかを判定しておく。
            bool alreadyOwned = inventory.HasItem(_item.ItemType);

            inventory.AddItem(_item);

            if (_item.ItemType != ItemType.ObstructionItem || !alreadyOwned)
                _uiPresenter?.Show(_item);

            AudioManager.Instance?.PlaySe(SeType.ItemPickup);

            if (_item.ItemType == ItemType.ObstructionItem && _respawner != null && _respawner.TryGetRespawnPosition(transform.position.y, _obstructionRespawnMinTiles, out var respawnPos))
            {
                transform.position = respawnPos;
                return;
            }
            
            gameObject.SetActive(false);
        }
    }
}
