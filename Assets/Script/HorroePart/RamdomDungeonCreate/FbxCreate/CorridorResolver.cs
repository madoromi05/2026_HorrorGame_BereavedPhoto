/// <summary>
/// 通路セルの上下左右の隣接状況からCorridorTypeと回転角度を返す
/// 判定はGridType.FloorとGridType.Doorを接続方向としてカウントする
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

    // 接続方向インデックス定数
    private const int kNorth = 0;
    private const int kEast = 1;
    private const int kSouth = 2;
    private const int kWest = 3;

    /// <summary>
    /// 指定セルのCorridorTypeと回転角度(Y軸)を返す
    /// </summary>
    public (CorridorType type, float rotationY) Resolve(GridType[,] grid, Vector2Int pos)
    {
        bool[] connected = new bool[4];

        for (int i = 0; i < kDirections.Length; i++)
        {
            var neighbor = pos + kDirections[i];
            if (!IsInGrid(grid, neighbor)) continue;

            var cellType = grid[neighbor.x, neighbor.y];
            connected[i] = cellType == GridType.Floor || cellType == GridType.Door;
        }

        return DetermineCorridorType(connected);
    }

    private (CorridorType type, float rotationY) DetermineCorridorType(bool[] connected)
    {
        bool north = connected[kNorth];
        bool east = connected[kEast];
        bool south = connected[kSouth];
        bool west = connected[kWest];

        int connectionCount = (north ? 1 : 0) + (east ? 1 : 0)
                            + (south ? 1 : 0) + (west ? 1 : 0);

        // 十字
        if (connectionCount == 4)
            return (CorridorType.Crossroad, 0f);

        // T字
        if (connectionCount == 3)
        {
            if (!west) return (CorridorType.T_Junction, 0f);
            if (!north) return (CorridorType.T_Junction, 90f);
            if (!east) return (CorridorType.T_Junction, 180f);
            if (!south) return (CorridorType.T_Junction, 270f);
        }

        // 直線・L字
        if (connectionCount == 2)
        {
            // 直線（南北）
            if (north && south) return (CorridorType.Straight, 0f);
            // 直線（東西）
            if (east && west) return (CorridorType.Straight, 90f);

            // L字
            if (north && east) return (CorridorType.Corner, 0f);
            if (east  && south) return (CorridorType.Corner, 90f);
            if (south && west) return (CorridorType.Corner, 180f);
            if (west  && north) return (CorridorType.Corner, 270f);
        }

        DebugCustom.LogWarning("CorridorResolver: 判定不能なパターンです");
        return (CorridorType.Straight, 0f);
    }

    private bool IsInGrid(GridType[,] grid, Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < grid.GetLength(0)
            && pos.y >= 0 && pos.y < grid.GetLength(1);
    }
}