using HorrorGame.Interaction;
using HorrorGame.Item;
using HorrorGame.UI;
using UnityEngine;

namespace HorrorGame.Dungeon
{
    /// <summary>
    /// ダンジョン生成後に部屋内のItemとUIの初期化を行うクラス。
    /// </summary>
    public class ItemInitializer : MonoBehaviour
    {
        [SerializeField] private MemoUIPresenter _memoUIPresenter;
        [SerializeField] private ItemAcquiredUIPresenter _itemAcquiredUIPresenter;
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private DungeonGenerator _dungeonGenerator;

        private void Start()
        {
            _dungeonGenerator.OnRoomPlaced += Initialize;
        }

        private void OnDestroy()
        {
            _dungeonGenerator.OnRoomPlaced -= Initialize;
        }

        /// <summary>
        /// roomParent配下の全MemoItem・ItemPickupにPresenterとInventoryを注入する。
        /// SectionPlacer.Place()の後に呼ぶこと。
        /// </summary>
        public void Initialize(Transform roomParent)
        {
            var inventory = _playerTransform.GetComponent<Inventory>();
            if (inventory == null)
            {
                DebugCustom.LogWarning("[ItemInitializer] Inventoryがプレイヤーに見つかりません。");
                return;
            }

            foreach (var memo in roomParent.GetComponentsInChildren<MemoItem>())
                memo.Init(_memoUIPresenter, inventory);

            foreach (var pickup in roomParent.GetComponentsInChildren<ItemPickup>())
                pickup.Init(_itemAcquiredUIPresenter);
        }
    }
}
