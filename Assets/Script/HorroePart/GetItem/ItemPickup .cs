using HorrorGame.Interaction;
using HorrorGame.UI;
using UnityEngine;

namespace HorrorGame.Item
{
    /// <summary>
    /// ワールドに配置されるアイテムオブジェクト。
    /// IInteractableを実装し、インタラクト時にInventoryへアイテムを追加する。
    /// 取得後は自身を非活性化してワールドから消す。
    /// Inspectorでitemをnullにすると何も起きないため必ず設定すること。
    /// </summary>
    [AddComponentMenu("HorrorGame/Item/ItemPickup")]
    [RequireComponent(typeof(Collider))]
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemData item;

        public bool CanInteract => item != null;
        public string HintText => item != null ? $"[E] {item.DisplayName}を取得" : string.Empty;

        private ItemAcquiredUIPresenter uiPresenter;

        public void Init(ItemAcquiredUIPresenter presenter)
        {
            uiPresenter = presenter;
        }
        /// <summary>
        /// プレイヤーのInventoryにアイテムを追加し、このGameObjectを非活性化する。
        /// Inventoryの取得にGetComponentInParentは使わず、PlayerInteractor経由で
        /// 渡された参照を使う設計のため、呼び出し元がInventoryを保持していること。
        /// </summary>
        public void OnInteract()
        {
            if (!CanInteract) return;

            // Inventoryはプレイヤー側が持つため、呼び出し元から渡してもらう設計だが
            // シンプルさ優先でFindで取得。規模が大きくなればDIやイベントバスに移行する。
            var inventory = FindFirstObjectByType<Inventory>();
            if (inventory == null)
            {
                DebugCustom.LogWarning("[ItemPickup] Inventoryが見つかりません。");
                return;
            }

            inventory.AddItem(item);
            uiPresenter?.Show(item);
            gameObject.SetActive(false);
        }
    }
}