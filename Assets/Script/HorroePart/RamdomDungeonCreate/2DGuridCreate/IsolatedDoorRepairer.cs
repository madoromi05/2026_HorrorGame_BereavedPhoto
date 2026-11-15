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
    private CorridorCarver _carver;

    /// <summary>
    /// 孤立 Door の補修を実行する。
    /// ConnectSections が全失敗した場合に備え、corridorCells が空なら強制接続を先行する。
    /// </summary>
    public void Repair(
        GridType[,] grid,
        Dictionary<SectionData, List<Vector2Int>> sectionDoorMap,
        int maxStraight)
    {
        _grid = grid;
        _carver = new CorridorCarver(grid, maxStraight);

        var corridorCells = new HashSet<Vector2Int>(GridUtil.CellsOf(_grid, GridType.Corridor));
        if (corridorCells.Count == 0)
        {
            DebugCustom.LogWarning("[IsolatedDoorRepairer] corridorCells が空のため強制接続を実行します");
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

        var target = GridUtil.FindNearest(exitCell.Value, corridorCells);
        if (target == null)
        {
            DebugCustom.LogWarning($"[IsolatedDoorRepairer] 孤立 Door {doorPos} の接続先が見つかりませんでした");
            return;
        }

        var path = _carver.Carve(exitCell.Value, target.Value);
        if (path == null)
        {
            DebugCustom.LogWarning($"[IsolatedDoorRepairer] 孤立 Door A* 失敗: {exitCell.Value} -> {target.Value}");
            return;
        }

        corridorCells.UnionWith(path);
    }

    /// <summary>
    /// Door の N/E/S/W 方向に Corridor が隣接していれば接続済みと判定する。
    /// </summary>
    private bool IsDoorConnected(Vector2Int doorPos)
    {
        foreach (var dir in GridUtil.FourDirs)
        {
            var neighbor = doorPos + dir;
            if (!GridUtil.IsInGrid(_grid, neighbor)) continue;
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
        foreach (var dir in GridUtil.FourDirs)
        {
            var neighbor = doorPos + dir;
            if (!GridUtil.IsInGrid(_grid, neighbor)) continue;
            if (_grid[neighbor.x, neighbor.y] == GridType.Empty)
                return neighbor;
        }
        return null;
    }
}