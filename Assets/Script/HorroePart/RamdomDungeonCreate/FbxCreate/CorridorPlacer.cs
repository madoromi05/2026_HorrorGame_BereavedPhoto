/// <summary>
/// GridType.Floor セルを CorridorResolver で判定し FBX を Instantiate する
/// 部屋内部のセルはスキップして廊下のみに配置する
/// </summary>
using DungeonSystem;
using UnityEngine;

public class CorridorPlacer
{
    private CorridorDataBase _corridorDataBase;
    private CorridorResolver _corridorResolver;
    private float _gridSize;

    public CorridorPlacer(CorridorDataBase corridorDataBase, float gridSize)
    {
        _corridorDataBase = corridorDataBase;
        _corridorResolver = new CorridorResolver();
        _gridSize = gridSize;
    }

    /// <summary>
    /// グリッド全体を走査し、廊下セル（部屋内部を除く Floor/Door）に Corridor を配置する
    /// </summary>
    public void Place(GridType[,] grid, Transform corridorParent)
    {
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                if (grid[x, y] != GridType.Corridor) continue;

                PlaceCorridor(grid, new Vector2Int(x, y), corridorParent);
            }
        }
    }

    private void PlaceCorridor(GridType[,] grid, Vector2Int gridPos, Transform corridorParent)
    {
        var (corridorType, rotationY) = _corridorResolver.Resolve(grid, gridPos);

        var prefab = _corridorDataBase.GetPrefab(corridorType);
        if (prefab == null) return;

        // セルの中心座標に配置する
        var worldPos = new Vector3(
            (gridPos.x + 0.5f) * _gridSize,
            0f,
            (gridPos.y + 0.5f) * _gridSize
        );

        var rotation = Quaternion.Euler(0f, rotationY, 0f);
        var instance = Object.Instantiate(prefab, worldPos, rotation, corridorParent);
        instance.name = $"Corridor_{corridorType}_{gridPos}";
    }
}