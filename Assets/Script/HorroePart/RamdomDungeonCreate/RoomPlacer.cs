using UnityEngine;
using System.Collections.Generic;

namespace DungeonSystem
{
    /// <summary>
    /// グリッドベースで部屋をランダム配置するクラス。
    /// 配置順: Start(1) → Boss(1) → Treasure → Rest → Elite → Normal
    /// </summary>
    public class RoomPlacer
    {
        private readonly OverlapChecker _overlapChecker;
        private readonly RoomTypeRule _roomTypeRule;

        public RoomPlacer(OverlapChecker overlapChecker, RoomTypeRule roomTypeRule)
        {
            _overlapChecker = overlapChecker;
            _roomTypeRule = roomTypeRule;
        }

        /// <summary>
        /// 全部屋を配置してDungeonDataのRoomsリストを構築する。
        /// </summary>
        public void Place(
            DungeonData data, DungeonConfig config, System.Random random)
        {
            float cellSize = config.GetCellSize();
            int columns = config.GridColumns;
            int rows = config.GridRows;

            // グリッドの占有状態を管理する
            var occupied = new bool[columns, rows];
            // セルインデックス → RoomData の対応（GraphBuilder用）
            var cellToRoom = new Dictionary<Vector2Int, RoomData>();
            int nextId = 0;

            // 配置優先順でRoomEntryを処理する
            var orderedEntries = GetOrderedEntries(config.RoomEntries);

            Vector2Int startIndex = Vector2Int.zero;

            foreach (var (entry, isStart) in orderedEntries)
            {
                int count = isStart ? 1 : entry.placementsNumber;

                for (int i = 0; i < count; ++i)
                {
                    Vector2Int cellIndex;

                    if (entry.roomType == RoomType.Start)
                    {
                        cellIndex = _roomTypeRule.DecideStartCell(
                            occupied, columns, rows, random);
                        startIndex = cellIndex;
                    }
                    else
                    {
                        if (!TryGetRandomEmptyCell(
                            occupied, columns, rows, random, out cellIndex))
                        {
                            Debug.LogWarning(
                                $"空きセルがないため {entry.roomType} の配置をスキップします");
                            continue;
                        }
                    }

                    // セル中心のワールド座標を計算する
                    var position = new Vector3(
                        cellIndex.x * cellSize,
                        0f,
                        cellIndex.y * cellSize);

                    // 接続ポイントはDungeonPlacerがInstantiate後に設定するため
                    // ここでは空リストを渡す
                    var room = new RoomData(
                        nextId++,
                        entry.roomType,
                        position,
                        entry.roomSize,
                        new List<ConnectionPointData>());

                    occupied[cellIndex.x, cellIndex.y] = true;
                    cellToRoom[cellIndex] = room;
                    data.Rooms.Add(room);
                }
            }
        }

        /// <summary>
        /// 配置優先順にRoomEntryを並べ直す。
        /// Startが先頭、Bossが2番目になるよう保証する。
        /// </summary>
        private IEnumerable<(RoomEntry entry, bool isStart)> GetOrderedEntries(
            IReadOnlyList<RoomEntry> entries)
        {
            // 優先順のリストで並び替える
            var priority = new[]
            {
                RoomType.Start,
                RoomType.Rest,
                RoomType.Normal,
            };

            foreach (var type in priority)
                foreach (var entry in entries)
                    if (entry.roomType == type)
                    {
                        // Startは複数countでも1として扱う
                        bool isStart = (type == RoomType.Start);
                        yield return (entry, isStart);
                    }
        }

        private bool TryGetRandomEmptyCell(
            bool[,] occupied, int columns, int rows,
            System.Random random, out Vector2Int result)
        {
            var emptyCells = new List<Vector2Int>();
            for (int r = 0; r < rows; ++r)
                for (int c = 0; c < columns; ++c)
                    if (!occupied[c, r])
                        emptyCells.Add(new Vector2Int(c, r));

            if (emptyCells.Count == 0)
            {
                result = Vector2Int.zero;
                return false;
            }

            result = emptyCells[random.Next(emptyCells.Count)];
            return true;
        }
    }
}