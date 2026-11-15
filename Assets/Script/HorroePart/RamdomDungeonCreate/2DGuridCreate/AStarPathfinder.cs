using DungeonSystem;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// グリッド上の A* 経路探索。CorridorCarver が通路を掘るときに使う
/// </summary>
public class AStarPathfinder
{
    private const int   kHashMultiplier           = 397;	// ハッシュ値を合成するときの乗数。よく使われる素数
    private const float kBaseMoveCost             = 1.0f;	// 何も無いセルへ進むコスト
    private const float kOpenCellMoveCost         = 0.5f;	// 既にあるドア・通路へ進むコスト。安くして既存の道に合流させる
    private const float kCorridorAdjacencyPenalty = 1.5f;	// 既存の通路の隣へ進むときに足すコスト。2 列の通路を作りにくくする

    /// <summary>
    /// 探索ノードの状態を表す構造体
    /// 位置、進行方向、直線継続数を保持する
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
            unchecked
            {
                int h = Pos.x;
                h = h * kHashMultiplier ^ Pos.y;
                h = h * kHashMultiplier ^ Dir.x;
                h = h * kHashMultiplier ^ Dir.y;
                return h * kHashMultiplier ^ Straight;
            }
        }
    }

    /// <summary>
    /// A* で start から end までの経路を返す。経路が存在しない場合は null
    /// maxStraight: 直線継続の上限（0 = 無制限）
    /// avoidCorridorAdjacency: 既存通路に隣接するセルのコストを上げて通路の密着を抑制する
    /// </summary>
    public List<Vector2Int> FindPath(
        GridType[,] grid,
        Vector2Int start,
        Vector2Int end,
        bool avoidCorridorAdjacency = false,
        int maxStraight = 0)
    {
        var openSet = CreateOpenSet();

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

            foreach (var dir in GridUtil.FourDirs)
            {
                var nPos = cur.Pos + dir;
                if (!GridUtil.IsInGrid(grid, nPos)) continue;

                var cellType = grid[nPos.x, nPos.y];
                if (!IsPassable(cellType)) continue;
                if (!TryGetStraight(grid, cur, dir, nPos, start, maxStraight, out int newStraight)) continue;

                float moveCost = MoveCost(grid, cur.Pos, nPos, cellType, avoidCorridorAdjacency);

                var next = new PathState(nPos, dir, newStraight);
                float newG = gCost[cur] + moveCost;
                if (gCost.TryGetValue(next, out float eg) && newG >= eg) continue;

                gCost[next] = newG;
                cameFrom[next] = cur;
                openSet.Add((newG + Heuristic(nPos, end), nextId));
                idState[nextId++] = next;
            }
        }

        // No path under the straight-run limit: retry unconstrained so we always connect.
        if (maxStraight > 0)
            return FindPath(grid, start, end, avoidCorridorAdjacency, 0);
        return null;
    }

    private static SortedSet<(float f, int id)> CreateOpenSet()	// f が小さい順。同じ f なら先に入れた順
        => new SortedSet<(float f, int id)>(
            Comparer<(float f, int id)>.Create((a, b) =>
                a.f != b.f ? a.f.CompareTo(b.f) : a.id.CompareTo(b.id)));

    private static bool IsPassable(GridType type)	// 壁と部屋の中は通らない
        => type != GridType.Wall && type != GridType.Floor;

    /// <summary>
    /// 次のセルまでの直進の長さを求める。maxStraight を超えるなら false
    /// </summary>
    private bool TryGetStraight(GridType[,] grid, PathState cur, Vector2Int dir, Vector2Int nPos,
                                Vector2Int start, int maxStraight, out int newStraight)
    {
        if (maxStraight <= 0)
        {
            newStraight = (dir == cur.Dir) ? cur.Straight + 1 : 1;
            return true;
        }

        newStraight = ComputeStraight(grid, cur, dir, start);
        // Span of the whole straight open run: path so far + existing open cells ahead.
        int span = newStraight + CountOpenCells(grid, nPos + dir, dir, maxStraight);
        return span <= maxStraight;
    }

    private float MoveCost(GridType[,] grid, Vector2Int from, Vector2Int to, GridType toType, bool avoidCorridorAdjacency)
    {
        float cost = (toType == GridType.Door || toType == GridType.Corridor) ? kOpenCellMoveCost : kBaseMoveCost;
        if (avoidCorridorAdjacency && toType == GridType.Empty && IsNextToCorridor(grid, to, from))
            cost += kCorridorAdjacencyPenalty;
        return cost;
    }

    private bool IsNextToCorridor(GridType[,] grid, Vector2Int cell, Vector2Int cameFrom)	// 来たセルを除く 4 方向に通路があるか
    {
        foreach (var dir in GridUtil.FourDirs)
        {
            var adj = cell + dir;
            if (adj == cameFrom) continue;
            if (!GridUtil.IsInGrid(grid, adj)) continue;
            if (grid[adj.x, adj.y] == GridType.Corridor) return true;
        }
        return false;
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

    // Straight-run length ending at the next cell. On the first step it seeds the
    // count with open cells (the Door and room Floor) behind the start, so a corridor
    // leaving a door also counts the room it looks straight back into.
    private int ComputeStraight(GridType[,] grid, PathState cur, Vector2Int dir, Vector2Int start)
    {
        if (cur.Dir == Vector2Int.zero)
            return CountOpenCells(grid, start, -dir, int.MaxValue) + 1;
        return (dir == cur.Dir) ? cur.Straight + 1 : 1;
    }

    // Counts contiguous open cells (Corridor/Door/Floor) from 'from' along 'dir', up to 'limit'.
    private int CountOpenCells(GridType[,] grid, Vector2Int from, Vector2Int dir, int limit)
    {
        int count = 0;
        var p = from;
        while (count < limit && GridUtil.IsInGrid(grid, p) && IsOpenCell(grid[p.x, p.y]))
        {
            count++;
            p += dir;
        }
        return count;
    }

    private bool IsOpenCell(GridType type)
        => type == GridType.Corridor || type == GridType.Door || type == GridType.Floor;
}
