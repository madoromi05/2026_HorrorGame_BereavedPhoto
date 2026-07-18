using DungeonSystem;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �O���b�h��� A* �o�H�T���BSectionConnector �� IsolatedDoorRepairer �����L����B
/// </summary>
public class AStarPathfinder
{
    /// <summary>
    /// �T���m�[�h�̏�Ԃ�\���\����
    /// �ʒu�A�i�s�����A�����p������ێ�����B
    /// </summary>
    private struct PathState : System.IEquatable<PathState>
    {
        public readonly Vector2Int Pos;
        public readonly Vector2Int Dir;
        public readonly int Straight;   // �����p����

        public PathState(Vector2Int pos, Vector2Int dir, int straight)
        { Pos = pos; Dir = dir; Straight = straight; }

        public bool Equals(PathState o) => Pos == o.Pos && Dir == o.Dir && Straight == o.Straight;
        public override bool Equals(object obj) => obj is PathState s && Equals(s);
        public override int GetHashCode()
        {
            //�n�b�V���l����������ۂ̏搔�Ƃ���397
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

    // �����̒�`�i�㉺���E�j
    private static readonly Vector2Int[] kDirs =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    /// <summary>
    /// A* �� start ���� end �܂ł̌o�H��Ԃ��B�o�H�����݂��Ȃ��ꍇ�� null�B
    /// maxStraight: �����p���̏���i0 = �������j�B
    /// avoidCorridorAdjacency: �����ʘH�ɗאڂ���Z���̃R�X�g���グ�ĒʘH�̖�����}������B
    /// </summary>
    public List<Vector2Int> FindPath(
        GridType[,] grid,
        Vector2Int start,
        Vector2Int end,
        bool avoidCorridorAdjacency = false,
        int maxStraight = 0)
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

                int newStraight;
                if (maxStraight <= 0)
                {
                    newStraight = (dir == cur.Dir) ? cur.Straight + 1 : 1;
                }
                else
                {
                    newStraight = ComputeStraight(grid, cur, dir, start);
                    // Span of the whole straight open run: path so far + existing open cells ahead.
                    int span = newStraight + CountOpenCells(grid, nPos + dir, dir, maxStraight);
                    if (span > maxStraight) continue;
                }

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

        // No path under the straight-run limit: retry unconstrained so we always connect.
        if (maxStraight > 0)
            return FindPath(grid, start, end, avoidCorridorAdjacency, 0);
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
        while (count < limit && IsInGrid(grid, p) && IsOpenCell(grid[p.x, p.y]))
        {
            count++;
            p += dir;
        }
        return count;
    }

    private bool IsOpenCell(GridType type)
        => type == GridType.Corridor || type == GridType.Door || type == GridType.Floor;

    private bool IsInGrid(GridType[,] grid, Vector2Int pos)
        => pos.x >= 0 && pos.x < grid.GetLength(0)
        && pos.y >= 0 && pos.y < grid.GetLength(1);
}