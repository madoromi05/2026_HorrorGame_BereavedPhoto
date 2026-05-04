using UnityEngine;

namespace DungeonSystem
{
    /// <summary>
    /// DungeonConfigが持つ部屋1種別の定義。
    /// PrefabとRoomTypeと配置数をセットで管理する。
    /// </summary>
    [System.Serializable]
    public class RoomEntry
    {
        public RoomType roomType;
        public GameObject prefab;

        // cellSize計算に使用する</summary>
        public Vector2 roomSize;

        public int PlacementsNunber;
    }
}