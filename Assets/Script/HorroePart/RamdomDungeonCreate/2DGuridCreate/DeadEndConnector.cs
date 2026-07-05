/// <summary>
/// 行き止まりの通路セルを検出し、最近傍の通路へ A* で接続する後処理クラス。
/// 隣接する Corridor/Door が 1 方向のみのセルを行き止まりと判定し、
/// 開口部方向の Empty セルを起点に最近傍の通路へ延伸する。
/// 全方向を試して接続できるまでリトライし、解消できる限りループする。
/// </summary>
using DungeonSystem;
using System.Collections.Generic;
using UnityEngine;

public class DeadEndConnector
{
    private GridType[,] _grid;
    private readonly AStarPathfinder _pathfinder = new AStarPathfinder();

    private static readonly Vector2Int[] kDirs =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    public void Connect(GridType[,] grid)
    {
        _grid = grid;

        const int kMaxPasses = 20;
        for (int pass = 0; pass < kMaxPasses; pass++)
        {
            var corridorCells = CollectCorridorCells();
            if (corridorCells.Count == 0) return;

            var deadEnds = FindDeadEnds();
            if (deadEnds.Count == 0) return;

            bool anyFixed = false;
            foreach (var (pos, openDirs) in deadEnds)
            {
                if (TryConnect(pos, openDirs, corridorCells))
                    anyFixed = true;
            }

            if (!anyFixed) break;
        }
    }

    private bool TryConnect(Vector2Int deadEnd, List<Vector2Int> openDirs, HashSet<Vector2Int> corridorCells)
    {
        foreach (var openDir in openDirs)
        {
            var exitCell = deadEnd + openDir;
            if (!IsInGrid(exitCell)) continue;
            if (_grid[exitCell.x, exitCell.y] != GridType.Empty) continue;

            var target = FindNearest(exitCell, corridorCells, deadEnd);
            if (target == null) continue;

            var path = _pathfinder.FindPath(_grid, exitCell, target.Value, avoidCorridorAdjacency: true);
            if (path == null) continue;

            _grid[exitCell.x, exitCell.y] = GridType.Corridor;
            foreach (var pos in path)
            {
                var cell = _grid[pos.x, pos.y];
                if (cell == GridType.Door || cell == GridType.Floor || cell == GridType.Corridor) continue;
                _grid[pos.x, pos.y] = GridType.Corridor;
            }

            corridorCells.Add(exitCell);
            foreach (var pos in path) corridorCells.Add(pos);
            return true;
        }
        return false;
    }

    // 行き止まりセルと全開口方向のリストを返す
    private List<(Vector2Int pos, List<Vector2Int> openDirs)> FindDeadEnds()
    {
        var result = new List<(Vector2Int, List<Vector2Int>)>();

        for (int x = 0; x < _grid.GetLength(0); x++)
        {
            for (int y = 0; y < _grid.GetLength(1); y++)
            {
                if (_grid[x, y] != GridType.Corridor) continue;
                var pos = new Vector2Int(x, y);

                int connCount = 0;
                var openDirs = new List<Vector2Int>();

                foreach (var dir in kDirs)
                {
                    var n = pos + dir;
                    if (!IsInGrid(n)) continue;
                    var cell = _grid[n.x, n.y];
                    if (cell == GridType.Corridor || cell == GridType.Door)
                        connCount++;
                    else if (cell == GridType.Empty)
                        openDirs.Add(dir);
                }

                if (connCount == 1 && openDirs.Count > 0)
                    result.Add((pos, openDirs));
            }
        }

        return result;
    }

    private Vector2Int? FindNearest(Vector2Int from, HashSet<Vector2Int> candidates, Vector2Int exclude)
    {
        Vector2Int? nearest = null;
        float minDist = float.MaxValue;

        foreach (var pos in candidates)
        {
            if (pos == exclude) continue;
            float dist = Vector2Int.Distance(from, pos);
            if (dist >= minDist) continue;
            minDist = dist;
            nearest = pos;
        }

        return nearest;
    }

    private HashSet<Vector2Int> CollectCorridorCells()
    {
        var cells = new HashSet<Vector2Int>();
        for (int x = 0; x < _grid.GetLength(0); x++)
            for (int y = 0; y < _grid.GetLength(1); y++)
                if (_grid[x, y] == GridType.Corridor)
                    cells.Add(new Vector2Int(x, y));
        return cells;
    }

    private bool IsInGrid(Vector2Int pos)
        => pos.x >= 0 && pos.x < _grid.GetLength(0)
        && pos.y >= 0 && pos.y < _grid.GetLength(1);
}
