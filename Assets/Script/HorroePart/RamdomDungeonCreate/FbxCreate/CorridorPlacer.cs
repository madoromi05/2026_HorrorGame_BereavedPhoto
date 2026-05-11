/// <summary>
/// GridType.FloorセルをCorridorResolverで判定しFBXをInstantiateする
/// 親オブジェクトはDungeonGeneratorから受け取る
/// </summary>
using DungeonSystem;
using UnityEngine;

public class CorridorPlacer
{
    private CorridorDataBase m_corridorDataBase;

    // 通路セルの隣接状況からCorridorTypeと回転角度を判定する
    private CorridorResolver m_corridorResolver;

    // 1グリッドのUnityワールド上のサイズ
    private float m_gridSize;

    public CorridorPlacer(CorridorDataBase corridorDataBase, float gridSize)
    {
        m_corridorDataBase = corridorDataBase;
        m_corridorResolver = new CorridorResolver();
        m_gridSize = gridSize;
    }

    /// <summary>
    /// グリッド全体を走査しGridType.FloorセルにCorridorFBXをInstantiateする
    /// </summary>
    public void Place(GridType[,] grid, Transform corridorParent)
    {
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                if (grid[x, y] != GridType.Floor) continue;
                PlaceCorridor(grid, new Vector2Int(x, y), corridorParent);
            }
        }
    }

    // 対象セルのCorridorTypeと回転角度を解決しPrefabを配置する
    private void PlaceCorridor(GridType[,] grid, Vector2Int gridPos, Transform corridorParent)
    {
        var (corridorType, rotationY) = m_corridorResolver.Resolve(grid, gridPos);

        var prefab = m_corridorDataBase.GetPrefab(corridorType);
        if (prefab == null) return;

        // 2DグリッドのXY座標を3DのXZ平面に変換する
        var worldPos = new Vector3(
            gridPos.x * m_gridSize,
            0f,
            gridPos.y * m_gridSize
        );

        var rotation = Quaternion.Euler(0f, rotationY, 0f);
        var instance = Object.Instantiate(prefab, worldPos, rotation, corridorParent);
        instance.name = $"Corridor_{corridorType}_{gridPos}";
    }
}