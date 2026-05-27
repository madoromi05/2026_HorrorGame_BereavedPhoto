using HorrorGame.Interaction;
using HorrorGame.Item;
using HorrorGame.UI;
using UnityEngine;

namespace HorrorGame.Dungeon
{
    /// <summary>
    /// ダンジョン生成後に部屋内のItemUIの初期化を行うクラス。
    /// </summary>
    public class ItemInitializer : MonoBehaviour
    {
        [SerializeField] private MemoUIPresenter memoUIPresenter;
        [SerializeField] private ItemAcquiredUIPresenter itemAcquiredUIPresenter;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private DungeonGenerator dungeonGenerator;

        private void Start()
        {
            dungeonGenerator.OnRoomPlaced += Initialize;
        }

        private void OnDestroy()
        {
            dungeonGenerator.OnRoomPlaced -= Initialize;
        }

        /// <summary>
        /// roomParent配下の全MemoItem・ItemPickupにPresenterとInventoryを注入する。
        /// SectionPlacer.Place()完了後に呼ぶこと。
        /// </summary>
        public void Initialize(Transform roomParent)
        {
            var inventory = playerTransform.GetComponent<Inventory>();
            if (inventory == null)
            {
                DebugCustom.LogWarning("[ItemInitializer] Inventoryがプレイヤーに見つかりません。");
                return;
            }

            foreach (var memo in roomParent.GetComponentsInChildren<MemoItem>())
                memo.Init(memoUIPresenter, inventory);

            foreach (var pickup in roomParent.GetComponentsInChildren<ItemPickup>())
                pickup.Init(itemAcquiredUIPresenter);
        }
    }
}