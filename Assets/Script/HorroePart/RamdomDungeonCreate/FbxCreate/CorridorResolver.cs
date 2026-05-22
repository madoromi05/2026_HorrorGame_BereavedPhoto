/// <summary>
/// 指定セルの隣接状況からCorridorTypeと回転角度(Y軸)を返す。
/// 隣接4方向にCorridor/Doorがあれば接続ありと判定する。
/// </summary>
using DungeonSystem;
using UnityEngine;

public class CorridorResolver
{
    private static readonly Vector2Int[] kDirections = new[]
    {
        Vector2Int.up,    // North
        Vector2Int.right, // East
        Vector2Int.down,  // South
        Vector2Int.left   // West
    };

    private const int kNorth = 0;
    private const int kEast = 1;
    private const int kSouth = 2;
    private const int kWest = 3;

    /// <summary>
    /// 一グリッド受け取り、隣接するセルの状態を解析してCorridorTypeと回転角度を返す。
    /// </summary>
    public (CorridorType type, float rotationY) Resolve(GridType[,] grid, Vector2Int pos)
    {
        bool[] connected = new bool[4];

        for (int i = 0; i < kDirections.Length; i++)
        {
            var neighbor = pos + kDirections[i];
            if (!IsInGrid(grid, neighbor)) continue;

            var cellType = grid[neighbor.x, neighbor.y];
            connected[i] = cellType == GridType.Corridor || cellType == GridType.Door;
        }

        return DetermineCorridorType(connected);
    }

    /// <summary>
    /// 指定座標の接続状態を解析し、配置すべきCorridorTypeと回転角度を返す。
    /// </summary>
    private (CorridorType type, float rotationY) DetermineCorridorType(bool[] connected)
    {
        bool north = connected[kNorth];
        bool east = connected[kEast];
        bool south = connected[kSouth];
        bool west = connected[kWest];

        int connectionCount = (north ? 1 : 0) + (east ? 1 : 0)
                            + (south ? 1 : 0) + (west ? 1 : 0);

        // 4方向すべて接続：十字路
        if (connectionCount == 4)
            return (CorridorType.Crossroad, 0f);

        // 3方向接続：T字路
        if (connectionCount == 3)
        {
            if (!west) return (CorridorType.T_Junction, 0f);
            if (!north) return (CorridorType.T_Junction, 90f);
            if (!east) return (CorridorType.T_Junction, 180f);
            if (!south) return (CorridorType.T_Junction, 270f);
        }

        // 2方向接続：直線またはコーナー
        if (connectionCount == 2)
        {
            if (north && south) return (CorridorType.Straight, 0f);
            if (east && west)  return (CorridorType.Straight, 90f);

            if (north && east) return (CorridorType.Corner, 0f);
            if (east  && south) return (CorridorType.Corner, 90f);
            if (south && west) return (CorridorType.Corner, 180f);
            if (west  && north) return (CorridorType.Corner, 270f);
        }

        // 行き止まり（1方向のみ接続）
        if (connectionCount == 1)
        {
            if (south) return (CorridorType.DeadEnd, 0f);   // 開口部South：南から接続
            if (west) return (CorridorType.DeadEnd, 90f);   // 開口部West：西から接続
            if (north) return (CorridorType.DeadEnd, 180f); // 開口部North：北から接続
            return (CorridorType.T_Junction, 270f);         // 開口部East：東から接続
        }

        // 孤立セル（接続なし）
        return (CorridorType.Straight, 0f);
    }

    private bool IsInGrid(GridType[,] grid, Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < grid.GetLength(0)
            && pos.y >= 0 && pos.y < grid.GetLength(1);
    }
}