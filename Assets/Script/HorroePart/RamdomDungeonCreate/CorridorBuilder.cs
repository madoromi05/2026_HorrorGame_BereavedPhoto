using UnityEngine;
using System.Collections.Generic;

namespace DungeonSystem
{
    /// <summary>
    /// グラフの各辺に対して通路の経路（ウェイポイント）を計算するクラス。
    /// L字/直線の2パターンを試し、部屋を貫通しない経路を優先して採用する。
    /// 全ペアを試しても有効な経路がない場合は警告ログを出してスキップする。
    /// </summary>
    public class CorridorBuilder
    {
        private readonly OverlapChecker _overlapChecker;

        public CorridorBuilder(OverlapChecker overlapChecker)
        {
            _overlapChecker = overlapChecker;
        }

        /// <summary>
        /// 各辺の接続ポイントペアを探索して通路経路を計算する。
        /// </summary>
        public void Build(
            DungeonData data,
            IReadOnlyList<GraphBuilder.Edge> edges,
            DungeonConfig config)
        {
            foreach (var edge in edges)
            {
                var roomA = FindRoom(data.Rooms, edge.RoomIdA);
                var roomB = FindRoom(data.Rooms, edge.RoomIdB);
                if (roomA == null || roomB == null) continue;

                if (!TryBuildCorridor(roomA, roomB, data.Rooms, config,
                    out var corridor))
                {
                    Debug.LogWarning(
                        $"Room {roomA.Id} - Room {roomB.Id} の有効な通路経路が見つかりません");
                    continue;
                }

                data.Corridors.Add(corridor);
            }
        }

        /// <summary>
        /// 2部屋間の有効な通路を探索する。
        /// 全接続ポイントペアを試し、貫通チェックをパスした最初の経路を採用する。
        /// </summary>
        private bool TryBuildCorridor(
            RoomData roomA,
            RoomData roomB,
            IReadOnlyList<RoomData> allRooms,
            DungeonConfig config,
            out CorridorData corridor)
        {
            corridor = null;

            foreach (var pointA in roomA.ConnectionPoints)
            {
                if (pointA.isUsed) continue;

                foreach (var pointB in roomB.ConnectionPoints)
                {
                    if (pointB.isUsed) continue;
                    // L字パターン1: X方向 → Z方向
                    var waypoints1 = MakeWaypointsPattern1(
                        pointA.position, pointB.position);
                    if (IsValidCorridor(waypoints1, allRooms, config,
                        roomA.Id, roomB.Id))
                    {
                        pointA.MarkUsed();
                        pointB.MarkUsed();
                        corridor = new CorridorData(pointA, pointB, waypoints1);
                        return true;
                    }

                    // L字パターン2: Z方向 → X方向
                    var waypoints2 = MakeWaypointsPattern2(
                        pointA.position, pointB.position);
                    if (IsValidCorridor(waypoints2, allRooms, config,
                        roomA.Id, roomB.Id))
                    {
                        pointA.MarkUsed();
                        pointB.MarkUsed();
                        corridor = new CorridorData(pointA, pointB, waypoints2);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// ウェイポイントの各セグメントが他の部屋と重なっていないか検証する。
        /// </summary>
        private bool IsValidCorridor(
            List<Vector3> waypoints,
            IReadOnlyList<RoomData> allRooms,
            DungeonConfig config,
            int excludeIdA,
            int excludeIdB)
        {
            for (int i = 0; i + 1 < waypoints.Count; ++i)
            {
                if (_overlapChecker.IsCorridorOverlappingAny(
                    waypoints[i], waypoints[i + 1],
                    config.CorridorWidth,
                    allRooms,
                    excludeIdA, excludeIdB))
                    return false;
            }
            return true;
        }

        /// <summary>X方向 → Z方向のL字ウェイポイントを生成する</summary>
        private List<Vector3> MakeWaypointsPattern1(Vector3 start, Vector3 end)
        {
            var mid = new Vector3(end.x, start.y, start.z);
            return new List<Vector3> { start, mid, end };
        }

        /// <summary>Z方向 → X方向のL字ウェイポイントを生成する</summary>
        private List<Vector3> MakeWaypointsPattern2(Vector3 start, Vector3 end)
        {
            var mid = new Vector3(start.x, start.y, end.z);
            return new List<Vector3> { start, mid, end };
        }

        private RoomData FindRoom(IReadOnlyList<RoomData> rooms, int id)
        {
            foreach (var room in rooms)
                if (room.Id == id) return room;
            return null;
        }
    }
}