/// <summary>
/// SectionDataをもとに部屋PrefabとプレイヤーをInstantiateする。
/// 敵の生成はEnemySpawnerに委譲しており、このクラスは部屋配置のみを担う。
/// </summary>
using DungeonSystem;
using UnityEngine;

public class SectionPlacer
{
    private readonly RoomDataBase _roomDataBase;
    private readonly float _gridSize;
    private readonly GameObject _playerPrefab;
    private readonly float _playerSpawnOffsetY;
    private readonly EnemySpawner _enemySpawner;

    public SectionPlacer(
        RoomDataBase roomDataBase,
        float gridSize,
        GameObject playerPrefab,
        float playerSpawnOffsetY,
        EnemySpawner enemySpawner)
    {
        _roomDataBase = roomDataBase;
        _gridSize = gridSize;
        _playerPrefab = playerPrefab;
        _playerSpawnOffsetY = playerSpawnOffsetY;
        _enemySpawner = enemySpawner;
    }

    /// <summary>
    /// 全セクションに部屋とプレイヤーを配置し、最後に敵を配置する。
    /// 敵配置をプレイヤー生成後に行うのは、EnemyControllerへPlayerTransformを注入するため。
    /// gridはEnemySpawner経由でMapWandererのウェイポイント構築に使用する。
    /// </summary>
    public void Place(SectionData[] sections, Transform roomParent, Transform enemyParent, GridType[,] grid, bool enemyLookDebug = false)
    {
        Transform playerTransform = null;

        foreach (var section in sections)
        {
            if (section.RoomGridData == null) continue;

            PlaceRoom(section, roomParent);

            if (section.Role == RoomType.Start)
                playerTransform = PlacePlayer(section, roomParent);
        }

        _enemySpawner.Place(sections, enemyParent, playerTransform, grid, enemyLookDebug);
    }

    private void PlaceRoom(SectionData section, Transform roomParent)
    {
        var prefab = _roomDataBase.GetPrefab(section.Role);
        if (prefab == null) return;

        var roomGridSize = section.RoomGridData.GridSize;
        var worldPos = new Vector3(
            (section.RoomGridPosition.x + (roomGridSize.x - 1) * 0.5f + 0.5f) * _gridSize,
            0f,
            (section.RoomGridPosition.y + (roomGridSize.y - 1) * 0.5f + 0.5f) * _gridSize
        );
        var instance = Object.Instantiate(prefab, worldPos, Quaternion.identity, roomParent);
        instance.name = $"Room_{section.Role}_{section.GridPosition}";
    }

    /// <summary>
    /// StartセクションのRoomGridDataに登録されたPlayerPositionsの先頭セルにプレイヤーを配置する。
    /// PlayerPositionsが未設定の場合は部屋中央にフォールバックする。
    /// </summary>
    private Transform PlacePlayer(SectionData section, Transform roomParent)
    {
        if (_playerPrefab == null) return null;

        var worldPos = ResolvePlayerWorldPosition(section);
        var instance = Object.Instantiate(_playerPrefab, worldPos, Quaternion.identity, roomParent);
        instance.name = "Player";
        return instance.transform;
    }

    private Vector3 ResolvePlayerWorldPosition(SectionData section)
    {
        var roomData = section.RoomGridData;
        if (roomData.PlayerPositions != null && roomData.PlayerPositions.Count > 0)
        {
            var localPos = roomData.PlayerPositions[0];
            return new Vector3(
                (section.RoomGridPosition.x + localPos.x + 0.5f) * _gridSize,
                _playerSpawnOffsetY,
                (section.RoomGridPosition.y + localPos.y + 0.5f) * _gridSize
            );
        }

        // PlayerPositions未設定時は部屋中央にフォールバック
        var gridSize = section.RoomGridData.GridSize;
        return new Vector3(
            (section.RoomGridPosition.x + (gridSize.x - 1) * 0.5f + 0.5f) * _gridSize,
            _playerSpawnOffsetY,
            (section.RoomGridPosition.y + (gridSize.y - 1) * 0.5f + 0.5f) * _gridSize
        );
    }
}