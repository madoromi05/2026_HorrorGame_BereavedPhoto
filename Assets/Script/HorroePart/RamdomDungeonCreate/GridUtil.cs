using System;
using System.Collections.Generic;
using DungeonSystem;
using UnityEngine;

/// <summary>
/// ダンジョンのグリッド座標の計算をまとめる係
/// 範囲の判定・4 方向・セルの収集・最近傍・セル中心のワールド座標を、生成の段ごとに書き直さないために置く
/// </summary>
public static class GridUtil
{
    private const float kCellCenterOffset = 0.5f;	// セルの角から中心までの距離（セル単位）

    private static readonly Vector2Int[] kFourDirs      = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
    private static readonly Vector2Int[] kClockwiseDirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

    public static ReadOnlySpan<Vector2Int> FourDirs      => kFourDirs;		// 上下左右。A* の探索の順に効くので並びを変えない
    public static ReadOnlySpan<Vector2Int> ClockwiseDirs => kClockwiseDirs;	// 北・東・南・西。番号で方角を指す所が使う

    public static bool IsInGrid(GridType[,] grid, Vector2Int pos)
        => pos.x >= 0 && pos.x < grid.GetLength(0)
        && pos.y >= 0 && pos.y < grid.GetLength(1);

    /// <summary>
    /// type のセルを、x の列ごとに y の順で返す
    /// </summary>
    public static IEnumerable<Vector2Int> CellsOf(GridType[,] grid, GridType type)
    {
        for (int x = 0; x < grid.GetLength(0); x++)
            for (int y = 0; y < grid.GetLength(1); y++)
                if (grid[x, y] == type)
                    yield return new Vector2Int(x, y);
    }

    /// <summary>
    /// from に最も近い候補を返す。距離が同じなら先に見つけた方を返す
    /// 候補が無ければ null を返す
    /// </summary>
    public static Vector2Int? FindNearest(Vector2Int from, IEnumerable<Vector2Int> candidates, Vector2Int? exclude = null)
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

    public static Vector3 CellCenterToWorld(Vector2 cell, float gridSize, float y)	// セル座標（小数可）の中心をワールド座標にする
        => new Vector3((cell.x + kCellCenterOffset) * gridSize, y, (cell.y + kCellCenterOffset) * gridSize);

    public static Vector2Int WorldToCell(Vector3 world, float gridSize)	// ワールド座標が入っているセル。CellCenterToWorld の逆
        => new Vector2Int(Mathf.FloorToInt(world.x / gridSize), Mathf.FloorToInt(world.z / gridSize));

    public static Vector2 RoomCenterCell(Vector2Int roomSize)	// 部屋の中央のセル座標。幅が偶数ならセルの境目になる
        => ((Vector2)roomSize - Vector2.one) / 2f;
}
