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

            // ワールド座標 → グリッドセル（セル中心規約の逆算）
            int playerX = Mathf.FloorToInt(_playerTransform.position.x / _gridSize);
            int playerY = Mathf.FloorToInt(_playerTransform.position.z / _gridSize);

            var candidates = new List<Vector2Int>();
            Vector2Int farthest = default;
            int farthestSqr = -1;
            bool hasWalkable = false;
            int minSqr = minTiles * minTiles;

            for (int x = 0; x < _grid.GetLength(0); x++)
            {
                for (int yy = 0; yy < _grid.GetLength(1); yy++)
                {
                    if (!IsWalkable(_grid[x, yy])) continue;
                    hasWalkable = true;

                    int dx = x - playerX;
                    int dy = yy - playerY;
                    int sqr = dx * dx + dy * dy;

                    if (sqr >= minSqr)
                        candidates.Add(new Vector2Int(x, yy));

                    if (sqr > farthestSqr)
                    {
                        farthestSqr = sqr;
                        farthest = new Vector2Int(x, yy);
                    }
                }
            }

            if (!hasWalkable) return false;

            // 10マス以上のセルがあればその中からランダム、無ければ最遠セルにフォールバック。
            Vector2Int cell = candidates.Count > 0
                ? candidates[Random.Range(0, candidates.Count)]
                : farthest;

            position = new Vector3(
                (cell.x + 0.5f) * _gridSize,
                y,
                (cell.y + 0.5f) * _gridSize);
            return true;
        }

        private static bool IsWalkable(GridType type) =>
            type == GridType.Floor ||
            type == GridType.Corridor ||
            type == GridType.Door ||
            type == GridType.PlayerPosition;
    }
}