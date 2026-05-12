/// <summary>
/// GridType.Floor セルを CorridorResolver で判定し FBX を Instantiate する
/// 部屋内部のセルはスキップして廊下のみに配置する
/// </summary>
using DungeonSystem;
using System.Collections.Generic;
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
    public void Place(GridType[,] grid, SectionData[] sections, Transform corridorParent)
    {
        var roomCells = BuildRoomCellSet(sections);

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                if (grid[x, y] != GridType.Floor && grid[x, y] != GridType.Door) continue;
                var pos = new Vector2Int(x, y);
                if (roomCells.Contains(pos)) continue;
                PlaceCorridor(grid, pos, corridorParent);
            }
        }
    }

    // 部屋内部セルの座標セットを構築する（部屋を持つセクションのみ）
    private HashSet<Vector2Int> BuildRoomCellSet(SectionData[] sections)
    {
        var cells = new HashSet<Vector2Int>();
        foreach (var section in sections)
        {
            if (section.RoomGridData == null) continue;
            var size = section.RoomGridData.GridSize;
            for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                    cells.Add(section.RoomGridPosition + new Vector2Int(x, y));
        }
        return cells;
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