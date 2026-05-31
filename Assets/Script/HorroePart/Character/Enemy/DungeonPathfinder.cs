using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵移動用ナビグリッド（bool[,]）上でA*経路探索を行うクラス。
/// EnemyNavGridBuilder で生成したグリッドを SetNavGrid で受け取る。
///
/// 性能のためオープンセットにバイナリミンヒープを使用（O(log n) の挿入・取り出し）。
/// 目標セルが非通行可の場合は最近傍の通行可セルへ BFS でスナップする。
/// パスは方向転換点のみ残して簡略化し、ウェイポイント追従のノード数を抑える。
/// </summary>
public class DungeonPathfinder
{
    private bool[,] _walkable;
    private float _navCellSize;
    private int _width;
    private int _height;

    private static readonly Vector2Int[] kDirs =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0),
        new Vector2Int(0, 1), new Vector2Int(0, -1),
    };

    public bool IsInitialized => _walkable != null;

    /// <summary>
    /// EnemyNavGridBuilder.Build の結果と navCellSize を受け取って初期化する。
    /// </summary>
    public void SetNavGrid(bool[,] walkable, float navCellSize)
    {
        _walkable    = walkable;
        _navCellSize = navCellSize;
        _width       = walkable.GetLength(0);
        _height      = walkable.GetLength(1);
    }

    /// <summary>
    /// ワールド座標 from→to の A* パスをワールド座標リストで返す。
    /// 目標セルが非通行可の場合は最近傍の通行可セルへスナップする。
    /// 経路が見つからない場合は { to } のみを返す（直進フォールバック）。
    /// </summary>
    public List<Vector3> FindPath(Vector3 from, Vector3 to, float y)
    {
        if (_walkable == null) return new List<Vector3> { to };

        var start = NearestWalkable(WorldToCell(from));
        var goal  = NearestWalkable(WorldToCell(to));

        if (start == goal)
            return new List<Vector3> { CellToWorld(goal, y) };

        var rawPath = AStarSearch(start, goal);
        if (rawPath == null || rawPath.Count == 0)
            return new List<Vector3> { to };

        var simplified = Simplify(rawPath);
        var result = new List<Vector3>(simplified.Count);
        foreach (var cell in simplified)
            result.Add(CellToWorld(cell, y));
        return result;
    }

    // ---- A* ----

    private List<Vector2Int> AStarSearch(Vector2Int start, Vector2Int goal)
    {
        var heap     = new MinHeap();
        var closed   = new HashSet<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore   = new Dictionary<Vector2Int, float> { [start] = 0f };

        heap.Push(H(start, goal), start);

        while (heap.Count > 0)
        {
            var current = heap.Pop();

            if (closed.Contains(current)) continue;
            closed.Add(current);

            if (current == goal)
                return Reconstruct(cameFrom, current);

            float curG = gScore.TryGetValue(current, out var cg) ? cg : float.MaxValue;

            foreach (var dir in kDirs)
            {
                var nb = current + dir;
                if (!IsWalkable(nb) || closed.Contains(nb)) continue;

                float tentG = curG + 1f;
                if (!gScore.TryGetValue(nb, out float existG) || tentG < existG)
                {
                    cameFrom[nb] = current;
                    gScore[nb]   = tentG;
                    heap.Push(tentG + H(nb, goal), nb);
                }
            }
        }

        return null;
    }

    private static List<Vector2Int> Reconstruct(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        var path = new List<Vector2Int> { current };
        while (cameFrom.TryGetValue(current, out var prev))
        {
            current = prev;
            path.Add(current);
        }
        path.Reverse();
        return path;
    }

    private static float H(Vector2Int a, Vector2Int b)
        => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    /// <summary>方向転換点のみ残してパスを短縮する。</summary>
    private static List<Vector2Int> Simplify(List<Vector2Int> path)
    {
        if (path.Count <= 2) return path;

        var result = new List<Vector2Int> { path[0] };
        for (int i = 1; i < path.Count - 1; i++)
        {
            var d1 = path[i]     - path[i - 1];
            var d2 = path[i + 1] - path[i];
            if (d1 != d2)
                result.Add(path[i]);
        }
        result.Add(path[path.Count - 1]);
        return result;
    }

    // ---- ユーティリティ ----

    private bool IsWalkable(Vector2Int c)
        => c.x >= 0 && c.x < _width && c.y >= 0 && c.y < _height && _walkable[c.x, c.y];

    private Vector2Int NearestWalkable(Vector2Int cell)
    {
        if (IsWalkable(cell)) return cell;

        var visited = new HashSet<Vector2Int> { cell };
        var queue   = new Queue<Vector2Int>();
        queue.Enqueue(cell);

        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            foreach (var d in kDirs)
            {
                var n = c + d;
                if (visited.Contains(n)) continue;
                visited.Add(n);
                if (IsWalkable(n)) return n;
                queue.Enqueue(n);
            }
        }

        return cell;
    }

    private Vector2Int WorldToCell(Vector3 world)
        => new Vector2Int(
            Mathf.FloorToInt(world.x / _navCellSize),
            Mathf.FloorToInt(world.z / _navCellSize)
        );

    private Vector3 CellToWorld(Vector2Int cell, float y)
        => new Vector3((cell.x + 0.5f) * _navCellSize, y, (cell.y + 0.5f) * _navCellSize);

    // ---- バイナリミンヒープ ----

    private sealed class MinHeap
    {
        private readonly List<(float priority, Vector2Int cell)> _data
            = new List<(float, Vector2Int)>();

        public int Count => _data.Count;

        public void Push(float priority, Vector2Int cell)
        {
            _data.Add((priority, cell));
            BubbleUp(_data.Count - 1);
        }

        public Vector2Int Pop()
        {
            var top  = _data[0].cell;
            int last = _data.Count - 1;
            _data[0] = _data[last];
            _data.RemoveAt(last);
            if (_data.Count > 0) SiftDown(0);
            return top;
        }

        private void BubbleUp(int i)
        {
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (_data[p].priority <= _data[i].priority) break;
                (_data[p], _data[i]) = (_data[i], _data[p]);
                i = p;
            }
        }

        private void SiftDown(int i)
        {
            int n = _data.Count;
            while (true)
            {
                int s = i, l = 2 * i + 1, r = 2 * i + 2;
                if (l < n && _data[l].priority < _data[s].priority) s = l;
                if (r < n && _data[r].priority < _data[s].priority) s = r;
                if (s == i) break;
                (_data[s], _data[i]) = (_data[i], _data[s]);
                i = s;
            }
        }
    }
}
