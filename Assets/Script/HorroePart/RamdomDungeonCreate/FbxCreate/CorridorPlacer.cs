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
    private CorridorMeshCombiner _corridorMeshCombiner;
    private float _gridSize;

    public CorridorPlacer(CorridorDataBase corridorDataBase, float gridSize)
    {
        _corridorDataBase = corridorDataBase;
        _corridorResolver = new CorridorResolver();
        _corridorMeshCombiner = new CorridorMeshCombiner();
        _gridSize = gridSize;
    }

    /// <summary>
    /// グリッド全体を走査し、廊下セル（部屋内部を除く Floor/Door）に Corridor を配置する
    /// </summary>
    public void Place(GridType[,] grid, Transform corridorParent)
    {
        foreach (var cell in GridUtil.CellsOf(grid, GridType.Corridor))
            PlaceCorridor(grid, cell, corridorParent);
    }

    /// <summary>
    /// 配置済みの通路メッシュをマテリアルごとに結合し、描画コール数を削減する
    /// NavMeshベイクはBoxColliderを参照するため、結合後もColliderは維持される
    /// </summary>
    public void Combine(Transform corridorParent)
    {
        _corridorMeshCombiner.Combine(corridorParent);
    }

    private void PlaceCorridor(GridType[,] grid, Vector2Int gridPos, Transform corridorParent)
    {
        var (corridorType, rotationY) = _corridorResolver.Resolve(grid, gridPos);

        var prefab = _corridorDataBase.GetPrefab(corridorType);
        if (prefab == null) return;

        var localPos = GridUtil.CellCenterToWorld(gridPos, _gridSize, 0f);	// 親のローカル空間でのセルの中心

        var instance = Object.Instantiate(prefab, corridorParent);

        // Prefab自身の元のPosition（Instantiate直後の値）をオフセットとしてセル中心座標に加算する
        var prefabOffset = instance.transform.localPosition;
        instance.transform.localPosition = localPos + prefabOffset;

        // Prefab自身の元の回転（Instantiate直後の値）に算出した回転を加算する
        // ワールド回転で設定するため、corridorParentの回転は結果に反映されない
        var baseRotation = instance.transform.rotation;
        instance.transform.rotation = Quaternion.Euler(0f, rotationY, 0f) * baseRotation;
        instance.name = $"Corridor_{corridorType}_{gridPos}";
    }
}