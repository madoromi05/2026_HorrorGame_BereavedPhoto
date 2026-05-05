using UnityEngine;
using System.Collections.Generic;

namespace DungeonSystem
{
    /// <summary>
    /// AABB（軸平行バウンディングボックス）による重なり判定クラス。
    /// 部屋同士・通路と部屋の重なりチェックに使用する。
    /// </summary>
    public class OverlapChecker
    {
        /// <summary>
        /// 2つの部屋が重なっているか判定する。
        /// マージンを加算することで隣接部屋間に最低限の隙間を確保する。
        /// </summary>
        public bool IsOverlapping(RoomData a, RoomData b, float margin = 0.1f)
        {
            return GetBoundsWithMargin(a, margin)
                .Intersects(GetBoundsWithMargin(b, margin));
        }

        // 配置済み部屋リストのいずれかと重なっているか判定する 
        public bool IsOverlappingAny(
            RoomData target, IReadOnlyList<RoomData> placedRooms, float margin = 0.1f)
        {
            foreach (var placed in placedRooms)
            {
                if (placed.Id == target.Id) continue;
                if (IsOverlapping(target, placed, margin)) return true;
            }
            return false;
        }

        /// <summary>
        /// 通路セグメント（始点・終点・幅）がいずれかの部屋と重なっているか判定する。
        /// 通路の始点・終点の部屋は判定から除外する。
        /// </summary>
        public bool IsCorridorOverlappingAny(
            Vector3 segmentStart,
            Vector3 segmentEnd,
            float corridorWidth,
            IReadOnlyList<RoomData> rooms,
            int excludeRoomIdA,
            int excludeRoomIdB)
        {
            var segmentBounds = CreateSegmentBounds(
                segmentStart, segmentEnd, corridorWidth);

            foreach (var room in rooms)
            {
                if (room.Id == excludeRoomIdA || room.Id == excludeRoomIdB) continue;
                if (segmentBounds.Intersects(room.GetBounds())) return true;
            }
            return false;
        }

        private Bounds GetBoundsWithMargin(RoomData room, float margin)
        {
            return new Bounds(
                room.Position,
                new Vector3(room.RoomSize.x + margin, 0f, room.RoomSize.y + margin));
        }

        /// <summary>
        /// セグメントの中心とサイズからBoundsを生成する。
        /// 水平・垂直セグメントのみ対応（L字は分割して呼び出す）。
        /// </summary>
        private Bounds CreateSegmentBounds(
            Vector3 start, Vector3 end, float width)
        {
            var center = (start + end) * 0.5f;
            var diff = end - start;
            var sizeX = Mathf.Abs(diff.x) + width;
            var sizeZ = Mathf.Abs(diff.z) + width;
            return new Bounds(center, new Vector3(sizeX, 0f, sizeZ));
        }
    }
}