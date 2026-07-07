using DungeonSystem;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// グリッド上の A* 経路探索。SectionConnector と IsolatedDoorRepairer が共有する。
/// </summary>
public class AStarPathfinder
{
    /// <summary>
    /// 探索ノードの状態を表す構造体
    /// 位置、進行方向、直線継続数を保持する。
    /// </summary>
    private struct PathState : System.IEquatable<PathState>
    {
        public readonly Vector2Int Pos;
        public readonly Vector2Int Dir;
        public readonly int Straight;   // 直線継続数

        public PathState(Vector2Int pos, Vector2Int dir, int straight)
        { Pos = pos; Dir = dir; Straight = straight; }

        public bool Equals(PathState o) => Pos == o.Pos && Dir == o.Dir && Straight == o.Straight;
        public override bool Equals(object obj) => obj is PathState s && Equals(s);
        public override int GetHashCode()
        {
            //ハッシュ値を合成する際の乗数として397
            unchecked
            {
                int h = Pos.x;
                h = h * 397 ^ Pos.y;
                h = h * 397 ^ Dir.x;
                h = h * 397 ^ Dir.y;
                return h * 397 ^ Straight;
            }
        }
    }

    // 方向の定義（上下左右）
    private static readonly Vector2Int[] kDirs =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    /// <summary>
    /// A* で start から end までの経路を返す。経路が存在しない場合は null。
    /// maxStraight: 直線継続の上限（0 = 無制限）。
    /// avoidCorridorAdjacency: 既存通路に隣接するセルのコストを上げて通路の密着を抑制する。
    /// </summary>
    public List<Vector2Int> FindPath(
        GridType[,] grid,
        Vector2Int start,
        Vector2Int end,
        bool avoidCorridorAdjacency = false)
    {
        var openSet = new SortedSet<(float f, int id)>(
            Comparer<(float f, int id)>.Create((a, b) =>
                a.f != b.f ? a.f.CompareTo(b.f) : a.id.CompareTo(b.id)));

        var idState = new Dictionary<int, PathState>();
        var gCost = new Dictionary<PathState, float>();
        var cameFrom = new Dictionary<PathState, PathState>();
        var closedSet = new HashSet<PathState>();
        int nextId = 0;

        var init = new PathState(start, Vector2Int.zero, 0);
        gCost[init] = 0f;
        openSet.Add((Heuristic(start, end), nextId));
        idState[nextId++] = init;

        while (openSet.Count > 0)
        {
            var (_, id) = openSet.Min;
            openSet.Remove(openSet.Min);
            var cur = idState[id];

            if (closedSet.Contains(cur)) continue;
            closedSet.Add(cur);

            if (cur.Pos == end)
                return BuildPath(cameFrom, cur);

            foreach (var dir in kDirs)
            {
                var nPos = cur.Pos + dir;
                if (!IsInGrid(grid, nPos)) continue;

                var cellType = grid[nPos.x, nPos.y];
                if (cellType == GridType.Wall) continue;
                if (cellType == GridType.Floor) continue;

                int newStraight = (dir == cur.Dir) ? cur.Straight + 1 : 1;

                float moveCost = cellType switch
                {
                    GridType.Door => 0.5f,
                    GridType.Corridor => 0.5f,
                    _ => 1.0f,
                };

                if (avoidCorridorAdjacency && cellType == GridType.Empty)
                {
                    foreach (var adjDir in kDirs)
                    {
                        var adj = nPos + adjDir;
                        if (adj == cur.Pos) continue;
                        if (!IsInGrid(grid, adj)) continue;
                        if (grid[adj.x, adj.y] == GridType.Corridor)
                        { moveCost += 1.5f; break; }
                    }
                }

                var next = new PathState(nPos, dir, newStraight);
                float newG = gCost[cur] + moveCost;
                if (gCost.TryGetValue(next, out float eg) && newG >= eg) continue;

                gCost[next] = newG;
                cameFrom[next] = cur;
                openSet.Add((newG + Heuristic(nPos, end), nextId));
                idState[nextId++] = next;
            }
        }

        return null;
    }

    private float Heuristic(Vector2Int a, Vector2Int b)
        => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    private List<Vector2Int> BuildPath(Dictionary<PathState, PathState> cameFrom, PathState end)
    {
        var path = new List<Vector2Int>();
        var cur = end;
        while (cameFrom.ContainsKey(cur))
        {
            path.Add(cur.Pos);
            cur = cameFrom[cur];
        }
        path.Add(cur.Pos);
        path.Reverse();
        return path;
    }

    private bool IsInGrid(GridType[,] grid, Vector2Int pos)
        => pos.x >= 0 && pos.x < grid.GetLength(0)
        && pos.y >= 0 && pos.y < grid.GetLength(1);
}