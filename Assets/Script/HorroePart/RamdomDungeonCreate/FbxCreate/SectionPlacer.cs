/// <summary>
/// SectionDataをもとに部屋Prefabをインスタンス化する
/// 各オブジェクトはDungeonGeneratorから受け取る
/// </summary>
using DungeonSystem;
using UnityEngine;

public class SectionPlacer
{
    private RoomDataBase _roomDataBase;
    private float _gridSize;

    public SectionPlacer(RoomDataBase roomDataBase, float gridSize)
    {
        _roomDataBase = roomDataBase;
        _gridSize = gridSize;
    }

    public void Place(SectionData[] sections, Transform roomParent)
    {
        foreach (var section in sections)
        {
            if (section.RoomGridData == null) continue;
            PlaceRoom(section, roomParent);
        }
    }

    private void PlaceRoom(SectionData section, Transform roomParent)
    {
        var prefab = _roomDataBase.GetPrefab(section.Role);
        if (prefab == null) return;

        // グリッド座標をUnityワールド座標に変換
        // GridPositionはセクションの左上グリッド座標。
        var roomGridSize = section.RoomGridData.GridSize;
        var worldPos = new Vector3(
            (section.RoomGridPosition.x + (roomGridSize.x - 1) * 0.5f + 0.5f) * _gridSize,
            0f,
            (section.RoomGridPosition.y + (roomGridSize.y - 1) * 0.5f + 0.5f) * _gridSize
        );

        DebugCustom.Log($"[Placer] RoomGridPosition={section.RoomGridPosition} GridSize={section.RoomGridData.GridSize}");
        DebugCustom.Log($"[Placer] worldPos={worldPos}");
        DebugCustom.Log($"[Placer] _gridSize={_gridSize}");

        var instance = Object.Instantiate(prefab, worldPos, Quaternion.identity, roomParent);
        instance.name = $"Room_{section.Role}_{section.GridPosition}";
    }
}
