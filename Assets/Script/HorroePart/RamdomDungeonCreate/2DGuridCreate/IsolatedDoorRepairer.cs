/// <summary>
/// セクション接続後も通路と繋がっていない Door を検出し、
/// 最近傍の通路セルへ向けて A* で通路を延伸する後処理クラス。
/// 全 Door が必ず通路ネットワークに参加することを保証する。
/// </summary>
using DungeonSystem;
using System.Collections.Generic;
using UnityEngine;

public class IsolatedDoorRepairer
{
    private GridType[,] _grid;

    /// <summary>
    /// 孤立 Door の補修を実行する。
    /// ConnectSections が全失敗した場合に備え、corridorCells が空なら強制接続を先行する。
    /// </summary>
    public void Repair(
        GridType[,] grid,
        Dictionary<SectionData, List<Vector2Int>> sectionDoorMap,
        SectionConnector connector,
        SectionData[] sections)
    {
        _grid = grid;

        var corridorCells = CollectCorridorCells();

        // ConnectSections が全失敗した場合は隣接セクションを順番に強制接続して通路を確保する
        if (corridorCells.Count == 0)
        {
            DebugCustom.LogWarning("[IsolatedDoorRepairer] corridorCells が空のため強制接続を実行します");
            for (int i = 0; i < sections.Length - 1; i++)
                connector.ConnectTwoSections(sections[i], sections[i + 1]);
            corridorCells = CollectCorridorCells();
        }

        // 強制接続後も Corridor が生成できなかった場合はマップ設定が不正なので中断する
        if (corridorCells.Count == 0)
        {
            DebugCustom.LogWarning("[IsolatedDoorRepairer] 強制接続後も corridorCells が空です。マップ設定を確認してください");
            return;
        }

        foreach (var (_, doors) in sectionDoorMap)
        {
            foreach (var doorPos in doors)
                TryRepairDoor(doorPos, corridorCells);
        }
    }

    /// <summary>
    /// 孤立している Door を 1 つ補修する。
    /// Door はFloor（部屋内部）に囲まれているため A* の起点にできない。
    /// 隣接する Empty セル（部屋外側方向）を起点に延伸することで
    /// 通路が部屋内部をすり抜けずに繋がる。
    /// </summary>
    private void TryRepairDoor(Vector2Int doorPos, HashSet<Vector2Int> corridorCells)
    {
        if (IsDoorConnected(doorPos)) return;

        var exitCell = FindExitCell(doorPos);
        if (exitCell == null)
        {
            DebugCustom.LogWarning($"[IsolatedDoorRepairer] Door {doorPos} の外側出口セルが見つかりません");
            return;
        }

        var target = FindNearestCorridorCell(exitCell.Value, corridorCells);
        if (target == null)
        {
            DebugCustom.LogWarning($"[IsolatedDoorRepairer] 孤立 Door {doorPos} の接続先が見つかりませんでした");
            return;
        }

        var path = RunAStar(exitCell.Value, target.Value);
        if (path == null)
        {
            DebugCustom.LogWarning($"[IsolatedDoorRepairer] 孤立 Door A* 失敗: {exitCell.Value} -> {target.Value}");
            return;
        }

        // 出口セルと経路を通路として書き込む
        _grid[exitCell.Value.x, exitCell.Value.y] = GridType.Corridor;
        foreach (var pos in path)
        {
            var cellType = _grid[pos.x, pos.y];
            if (cellType == GridType.Door) continue;
            if (cellType == GridType.Floor) continue;
            if (cellType == GridType.Corridor) continue;
            _grid[pos.x, pos.y] = GridType.Corridor;
        }

        corridorCells.Add(exitCell.Value);
        corridorCells.UnionWith(path);
    }

    /// <summary>
    /// Door の N/E/S/W 方向に Corridor が隣接していれば接続済みと判定する。
    /// </summary>
    private bool IsDoorConnected(Vector2Int doorPos)
    {
        foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
        {
            var neighbor = doorPos + dir;
            if (!IsInGrid(neighbor)) continue;
            if (_grid[neighbor.x, neighbor.y] == GridType.Corridor) return true;
        }
        return false;
    }

    /// <summary>
    /// Door に隣接する Empty セル（部屋外側方向の出口）を返す。
    /// 全方向 Empty でなければ null を返す。
    /// </summary>
    private Vector2Int? FindExitCell(Vector2Int doorPos)
    {
        foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
        {
            var neighbor = doorPos + dir;
            if (!IsInGrid(neighbor)) continue;
            if (_grid[neighbor.x, neighbor.y] == GridType.Empty)
                return neighbor;
        }
        return null;
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

    private Vector2Int? FindNearestCorridorCell(Vector2Int from, HashSet<Vector2Int> candidates)
    {
        Vector2Int? nearest = null;
        float minDist = float.MaxValue;

        foreach (var pos in candidates)
        {
            float dist = Vector2Int.Distance(from, pos);
            if (dist >= minDist) continue;
            minDist = dist;
            nearest = pos;
        }

        return nearest;
    }

    /// <summary>
    /// A* で start から end までの経路を返す。
    /// Wall と部屋内部（Door 以外の Floor）は通過不可。
    /// 経路が見つからない場合は null を返す。
    /// </summary>
    private List<Vector2Int> RunAStar(Vector2Int start, Vector2Int end)
    {
        var openSet = new SortedSet<(float f, Vector2Int pos)>(
            Comparer<(float f, Vector2Int pos)>.Create((a, b) =>
                a.f != b.f ? a.f.CompareTo(b.f) :
                a.pos.x != b.pos.x ? a.pos.x.CompareTo(b.pos.x) :
                a.pos.y.CompareTo(b.pos.y)
            )
        );

        var gCost = new Dictionary<Vector2Int, float>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        gCost[start] = 0f;
        openSet.Add((Heuristic(start, end), start));

        var neighbors = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        int iterationCount = 0;
        const int kMaxIterations = 100;

        while (openSet.Count > 0)
        {
            if (++iterationCount > kMaxIterations)
            {
                DebugCustom.LogWarning($"[IsolatedDoorRepairer] A*探索が上限({kMaxIterations}回)に達したため強制中断しました。Start:{start} End:{end}");
                break;
            }

            var (_, current) = openSet.Min;
            openSet.Remove(openSet.Min);

            if (current == end)
                return BuildPath(cameFrom, current);

            foreach (var dir in neighbors)
            {
                var neighbor = current + dir;
                if (!IsInGrid(neighbor)) continue;

                var cellType = _grid[neighbor.x, neighbor.y];
                if (cellType == GridType.Wall) continue;
                if (cellType == GridType.Floor) continue;

                float moveCost = cellType switch
                {
                    GridType.Door => 0.5f,
                    GridType.Corridor => 0.5f,
                    _ => 1.0f,
                };

                float newG = gCost[current] + moveCost;
                if (gCost.TryGetValue(neighbor, out float existingG) && newG >= existingG) continue;

                gCost[neighbor] = newG;
                cameFrom[neighbor] = current;
                openSet.Add((newG + Heuristic(neighbor, end), neighbor));
            }
        }

        return null;
    }

    private float Heuristic(Vector2Int a, Vector2Int b)
        => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    private List<Vector2Int> BuildPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        var path = new List<Vector2Int>();
        while (cameFrom.ContainsKey(current))
        {
            path.Add(current);
            current = cameFrom[current];
        }
        path.Add(current);
        path.Reverse();
        return path;
    }

    private bool IsInGrid(Vector2Int pos)
        => pos.x >= 0 && pos.x < _grid.GetLength(0)
        && pos.y >= 0 && pos.y < _grid.GetLength(1);
}