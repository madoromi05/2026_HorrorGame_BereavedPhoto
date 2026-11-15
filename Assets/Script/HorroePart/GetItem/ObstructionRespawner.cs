using System.Collections.Generic;
using DungeonSystem;
using UnityEngine;

namespace HorrorGame.Item
{
    /// <summary>
    /// 妨害アイテム取得時に、プレイヤーから一定マス数以上離れた
    /// 歩行可能セルへ再配置するためのワールド座標を計算するクラス。
    /// グリッド座標系は OfudaPlacer / CorridorPlacer と同じ「セル中心」規約に従う。
    /// </summary>
    public class ObstructionRespawner
    {
        private readonly GridType[,] _grid;
        private readonly float _gridSize;
        private readonly Transform _playerTransform;

        public ObstructionRespawner(GridType[,] grid, float gridSize, Transform playerTransform)
        {
            _grid = grid;
            _gridSize = gridSize;
            _playerTransform = playerTransform;
        }

        /// <summary>
        /// プレイヤーから minTiles マス以上離れた歩行可能セルをランダムに1つ選び、
        /// そのセル中心のワールド座標（Y は呼び出し元指定）を返す。
        /// 条件を満たすセルが無い場合は最も遠いセルを採用する。
        /// 歩行可能セルが全く無い場合のみ false。
        /// </summary>
        public bool TryGetRespawnPosition(float y, int minTiles, out Vector3 position)
        {
            position = default;
            if (_grid == null || _playerTransform == null) return false;

            var playerCell = GridUtil.WorldToCell(_playerTransform.position, _gridSize);
            if (!TryPickCell(playerCell, minTiles, out var cell)) return false;

            position = GridUtil.CellCenterToWorld(cell, _gridSize, y);
            return true;
        }

        /// <summary>
        /// minTiles マス以上離れた歩行可能セルからランダムに 1 つ選ぶ。無ければ最も遠いセルを選ぶ
        /// 歩行可能セルが 1 つも無ければ false
        /// </summary>
        private bool TryPickCell(Vector2Int playerCell, int minTiles, out Vector2Int cell)
        {
            var candidates = new List<Vector2Int>();
            Vector2Int farthest = default;
            int farthestSqr = -1;
            bool hasWalkable = false;
            int minSqr = minTiles * minTiles;

            for (int x = 0; x < _grid.GetLength(0); x++)
            {
                for (int y = 0; y < _grid.GetLength(1); y++)
                {
                    if (!IsWalkable(_grid[x, y])) continue;
                    hasWalkable = true;

                    int dx = x - playerCell.x;
                    int dy = y - playerCell.y;
                    int sqr = dx * dx + dy * dy;

                    if (sqr >= minSqr)
                        candidates.Add(new Vector2Int(x, y));

                    if (sqr > farthestSqr)
                    {
                        farthestSqr = sqr;
                        farthest = new Vector2Int(x, y);
                    }
                }
            }

            cell = candidates.Count > 0
                ? candidates[Random.Range(0, candidates.Count)]
                : farthest;
            return hasWalkable;
        }

        private static bool IsWalkable(GridType type) =>
            type == GridType.Floor ||
            type == GridType.Corridor ||
            type == GridType.Door ||
            type == GridType.PlayerPosition;
    }
}