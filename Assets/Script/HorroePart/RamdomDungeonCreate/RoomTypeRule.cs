using UnityEngine;
using System.Collections.Generic;

namespace DungeonSystem
{
    /// <summary>
    /// RoomTypeごとの配置ルールを定義するクラス。
    /// 配置先のセル選択ロジックのみを担当する。
    /// </summary>
    public class RoomTypeRule
    {
        /// <summary>
        /// Startは端セルに1つだけ配置する。
        /// RoomEntryのcountが複数でも1つに制限する。
        /// </summary>
        public Vector2Int DecideStartCell(
            bool[,] occupied, int columns, int rows, System.Random random)
        {
            var edgeCells = new List<Vector2Int>();
            for (int r = 0; r < rows; ++r)
                for (int c = 0; c < columns; ++c)
                    if (IsEdgeCell(c, r, columns, rows) && !occupied[c, r])
                        edgeCells.Add(new Vector2Int(c, r));

            Debug.Assert(edgeCells.Count > 0, "端セルが存在しません");
            return edgeCells[random.Next(edgeCells.Count)];
        }

        /// <summary>
        /// BFSでStartから最も遠い空きセルをBoss配置先とする。
        /// StartとBossの距離を最大化することで難易度設計を安定させる。
        /// </summary>
        public Vector2Int DecideBossCell(
            bool[,] occupied, int columns, int rows,
            Vector2Int startIndex)
        {
            var dist = new int[columns, rows];
            for (int r = 0; r < rows; ++r)
                for (int c = 0; c < columns; ++c)
                    dist[c, r] = -1;

            var queue = new Queue<Vector2Int>();
            dist[startIndex.x, startIndex.y] = 0;
            queue.Enqueue(startIndex);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in GetNeighbors(current, columns, rows))
                {
                    if (dist[neighbor.x, neighbor.y] == -1)
                    {
                        dist[neighbor.x, neighbor.y] =
                            dist[current.x, current.y] + 1;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            int maxDist = -1;
            var bossIndex = Vector2Int.zero;
            for (int r = 0; r < rows; ++r)
                for (int c = 0; c < columns; ++c)
                    if (!occupied[c, r] && dist[c, r] > maxDist)
                    {
                        maxDist = dist[c, r];
                        bossIndex = new Vector2Int(c, r);
                    }

            Debug.Assert(maxDist >= 0, "Bossセルが見つかりません");
            return bossIndex;
        }

        private bool IsEdgeCell(int col, int row, int columns, int rows)
        {
            return col == 0 || col == columns - 1
                || row == 0 || row == rows - 1;
        }

        private IEnumerable<Vector2Int> GetNeighbors(
            Vector2Int index, int columns, int rows)
        {
            var directions = new[]
            {
                Vector2Int.up, Vector2Int.down,
                Vector2Int.left, Vector2Int.right
            };

            foreach (var dir in directions)
            {
                var neighbor = index + dir;
                if (neighbor.x >= 0 && neighbor.x < columns
                 && neighbor.y >= 0 && neighbor.y < rows)
                    yield return neighbor;
            }
        }
    }
}