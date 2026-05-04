using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

namespace DungeonSystem
{
    /// <summary>
    /// 部屋1つのデータ。MonoBehaviourに依存しない純粋C#クラス。
    /// Prefabへの参照は持たない（DungeonConfigが管理する）。
    /// </summary>
    public class RoomData
    {
        public int Id { get; private set; }
        public RoomType RoomType { get; private set; }
        public Vector3 Position { get; private set; }
        public Vector2 RoomSize { get; private set; }
        public IReadOnlyCollection<ConnectionPointData> ConnectionPoints { get; private set; }

        public RoomData(
            int id,
            RoomType roomType,
            Vector3 position,
            Vector2 roomSize,
            List<ConnectionPointData> connectionPoints)
        {
            Id = id;
            RoomType = roomType;
            Position = position;
            RoomSize = roomSize;
            ConnectionPoints = connectionPoints;
        }
        public Bounds GetBounds()
        {
            return new Bounds(
                Position,
                new Vector3(RoomSize.x, 0f, Size.y));
        }
    }
}