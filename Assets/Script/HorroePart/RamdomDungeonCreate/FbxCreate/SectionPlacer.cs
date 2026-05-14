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
    private GameObject _playerPrefab;
    private float _playerSpawnOffsetY;

    public SectionPlacer(RoomDataBase roomDataBase, float gridSize, GameObject playerPrefab, float playerSpawnOffsetY)
    {
        _roomDataBase = roomDataBase;
        _gridSize = gridSize;
        _playerPrefab = playerPrefab;
        _playerSpawnOffsetY = playerSpawnOffsetY;
    }

    public void Place(SectionData[] sections, Transform roomParent)
    {
        foreach (var section in sections)
        {
            if (section.RoomGridData == null) continue;
            PlaceRoom(section, roomParent);

            if (section.Role == RoomType.Start)
                PlacePlayer(section, roomParent);
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
             0,
            (section.RoomGridPosition.y + (roomGridSize.y - 1) * 0.5f + 0.5f) * _gridSize
        );

        DebugCustom.Log($"[Placer] RoomGridPosition={section.RoomGridPosition} GridSize={section.RoomGridData.GridSize}");
        DebugCustom.Log($"[Placer] worldPos={worldPos}");
        DebugCustom.Log($"[Placer] _gridSize={_gridSize}");

        var instance = Object.Instantiate(prefab, worldPos, Quaternion.identity, roomParent);
        instance.name = $"Room_{section.Role}_{section.GridPosition}";
    }

    /// <summary>
    /// Start セクションの RoomGridData に登録された PlayerPositions の先頭セルにプレイヤーを配置する。
    /// PlayerPositions が未設定の場合は部屋中央にフォールバックする。
    /// </summary>
    private void PlacePlayer(SectionData section, Transform roomParent)
    {
        if (_playerPrefab == null) return;

        var worldPos = ResolvePlayerWorldPosition(section);
        var instance = Object.Instantiate(_playerPrefab, worldPos, Quaternion.identity, roomParent);
        instance.name = "Player";
    }

    private Vector3 ResolvePlayerWorldPosition(SectionData section)
    {
        var roomData = section.RoomGridData;
        if (roomData.PlayerPositions != null && roomData.PlayerPositions.Count > 0)
        {
            // グリッドローカル座標 → ワールド座標
            var localPos = roomData.PlayerPositions[0];
            return new Vector3(
                (section.RoomGridPosition.x + localPos.x + 0.5f) * _gridSize,
                _playerSpawnOffsetY,
                (section.RoomGridPosition.y + localPos.y + 0.5f) * _gridSize
            );
        }

        // PlayerPositions 未設定時は部屋中央にフォールバック
        DebugCustom.LogWarning("[SectionPlacer] PlayerPositions が未設定のため部屋中央に配置します");
        var gridSize = roomData.GridSize;
        return new Vector3(
            (section.RoomGridPosition.x + (gridSize.x - 1) * 0.5f + 0.5f) * _gridSize,
            _playerSpawnOffsetY,
            (section.RoomGridPosition.y + (gridSize.y - 1) * 0.5f + 0.5f) * _gridSize
        );
    }
}
