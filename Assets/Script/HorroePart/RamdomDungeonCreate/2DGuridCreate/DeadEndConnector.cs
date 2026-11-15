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
    private CorridorCarver _carver;

    public void Connect(GridType[,] grid, int maxStraight)
    {
        _grid = grid;
        _carver = new CorridorCarver(grid, maxStraight);

        const int kMaxPasses = 20;
        for (int pass = 0; pass < kMaxPasses; pass++)
        {
            var corridorCells = new HashSet<Vector2Int>(GridUtil.CellsOf(_grid, GridType.Corridor));
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
            if (!GridUtil.IsInGrid(_grid, exitCell)) continue;
            if (_grid[exitCell.x, exitCell.y] != GridType.Empty) continue;

            var target = GridUtil.FindNearest(exitCell, corridorCells, deadEnd);
            if (target == null) continue;

            var path = _carver.Carve(exitCell, target.Value);
            if (path == null) continue;

            corridorCells.UnionWith(path);
            return true;
        }
        return false;
    }

    // 行き止まりセルと全開口方向のリストを返す
    private List<(Vector2Int pos, List<Vector2Int> openDirs)> FindDeadEnds()
    {
        var result = new List<(Vector2Int, List<Vector2Int>)>();

        foreach (var pos in GridUtil.CellsOf(_grid, GridType.Corridor))
        {
            int connCount = 0;
            var openDirs = new List<Vector2Int>();

            foreach (var dir in GridUtil.FourDirs)
            {
                var n = pos + dir;
                if (!GridUtil.IsInGrid(_grid, n)) continue;
                var cell = _grid[n.x, n.y];
                if (cell == GridType.Corridor || cell == GridType.Door)
                    connCount++;
                else if (cell == GridType.Empty)
                    openDirs.Add(dir);
            }

            if (connCount == 1 && openDirs.Count > 0)
                result.Add((pos, openDirs));
        }

        return result;
    }
}
