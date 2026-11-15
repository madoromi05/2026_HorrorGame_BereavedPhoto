/// <summary>
/// 通路（GridType.Corridor）セルの中からランダムに選んだ位置へお札（ItemPickupプレハブ）を配置する。
/// EnemySpawner / CorridorPlacer と同じく、DungeonGenerator から呼ばれる配置専用クラス。
/// 取得処理・UI初期化は既存の ItemPickup / ItemInitializer の仕組みに委ねる。
/// </summary>
using System.Collections.Generic;
using DungeonSystem;
using UnityEngine;

public class OfudaPlacer
{
    private readonly GameObject _ofudaPrefab;
    private readonly float _gridSize;
    private readonly int _count;
    private readonly float _spawnOffsetY;

    public OfudaPlacer(GameObject ofudaPrefab, float gridSize, int count, float spawnOffsetY)
    {
        _ofudaPrefab  = ofudaPrefab;
        _gridSize     = gridSize;
        _count        = count;
        _spawnOffsetY = spawnOffsetY;
    }

    /// <summary>
    /// グリッド上の通路セルを収集し、重複なくランダムに選んだセル中心へお札を配置する。
    /// 通路セルが _count 未満の場合は、存在する分だけ配置する。
    /// </summary>
    public void Place(GridType[,] grid, Transform ofudaParent)
    {
        if (_ofudaPrefab == null || _count <= 0) return;

        var corridorCells = new List<Vector2Int>(GridUtil.CellsOf(grid, GridType.Corridor));
        if (corridorCells.Count == 0) return;

        int placeCount = Mathf.Min(_count, corridorCells.Count);
        Shuffle(corridorCells);

        for (int i = 0; i < placeCount; i++)
            PlaceOfuda(corridorCells[i], ofudaParent, i);
    }

    // Fisher-Yates シャッフル。先頭 placeCount 個をランダム抽出するために全体を並び替える。
    private static void Shuffle(List<Vector2Int> cells)
    {
        for (int i = cells.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (cells[i], cells[j]) = (cells[j], cells[i]);
        }
    }

    private void PlaceOfuda(Vector2Int gridPos, Transform ofudaParent, int index)
    {
        var worldPos = GridUtil.CellCenterToWorld(gridPos, _gridSize, _spawnOffsetY);

        // プレハブに設定された角度をそのまま反映する（無回転で固定しない）。
        var instance = Object.Instantiate(_ofudaPrefab, worldPos, _ofudaPrefab.transform.rotation, ofudaParent);
        instance.name = $"Ofuda_{gridPos}_{index}";
    }
}
