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
        [SerializeField] private ItemGetUIPresenter _itemGetUIPresenter;
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private DungeonGenerator _dungeonGenerator;

        private void Awake()
        {
            DebugCustom.ValidateFields(this,
                (nameof(_memoUIPresenter), _memoUIPresenter),
                (nameof(_itemGetUIPresenter), _itemGetUIPresenter),
                (nameof(_playerTransform), _playerTransform),
                (nameof(_dungeonGenerator), _dungeonGenerator));
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
                // 妨害アイテムを取得時に再配置するための情報を組み立てる。
                var respawner = new ObstructionRespawner(
                _dungeonGenerator.Grid,
                _dungeonGenerator.GridSize,
                _playerTransform);

            foreach (var memo in roomParent.GetComponentsInChildren<MemoItem>())
                memo.Init(_memoUIPresenter, inventory);

            foreach (var pickup in roomParent.GetComponentsInChildren<ItemPickup>())
                pickup.Init(_itemGetUIPresenter, respawner);

            // 通路にランダム配置されたお札（ItemPickup）も初期化する。
            var ofudaParent = _dungeonGenerator.OfudaParent;
            if (ofudaParent != null)
            {
                foreach (var pickup in ofudaParent.GetComponentsInChildren<ItemPickup>())
                    pickup.Init(_itemGetUIPresenter, respawner);
            }
        }
    }
}
