using System.Collections.Generic;
using DungeonSystem;
using UnityEngine;

/// <summary>
/// A* で経路を探し、通る道を Corridor としてグリッドへ書き込む部品。
/// 部屋の中・ドア・既にある通路は書き換えない。
/// </summary>
public class CorridorCarver
{
    private readonly AStarPathfinder _pathfinder = new AStarPathfinder();
    private readonly GridType[,] _grid;
    private readonly int _maxStraight;	// 一直線に続けてよいマスの上限。0 なら上限なし

    public CorridorCarver(GridType[,] grid, int maxStraight)
    {
        _grid = grid;
        _maxStraight = maxStraight;
    }

    /// <summary>
    /// start から end へ通路を掘り、通った道を返す。道は start と end を含む。
    /// 道が見つからなければ、何も書き換えずに null を返す。
    /// </summary>
    public List<Vector2Int> Carve(Vector2Int start, Vector2Int end)
    {
        var path = _pathfinder.FindPath(_grid, start, end, avoidCorridorAdjacency: true, maxStraight: _maxStraight);
        if (path == null) return null;

        foreach (var pos in path)
            if (IsCarvable(_grid[pos.x, pos.y]))
                _grid[pos.x, pos.y] = GridType.Corridor;
        return path;
    }

    private static bool IsCarvable(GridType type)	// 部屋の中・ドア・既にある通路は残す
        => type != GridType.Door && type != GridType.Floor && type != GridType.Corridor;
}
