/// <summary>
/// SectionDataをもとに部屋Prefabをインスタンス化する
/// 各オブジェクトはDungeonGeneratorから受け取る
/// </summary>
using DungeonSystem;
using System.Linq;
using UnityEngine;

public class SectionPlacer
{
    private RoomDataBase _roomDataBase;
    private float _gridSize;
    private GameObject _playerPrefab;
    private float _playerSpawnOffsetY;
    private GameObject _enemyPrefab;
    private float _enemySpawnOffsetY;

    public SectionPlacer(RoomDataBase roomDataBase, float gridSize, GameObject playerPrefab, float playerSpawnOffsetY, GameObject enemyPrefab, float enemySpawnOffsetY)
    {
        _roomDataBase = roomDataBase;
        _gridSize = gridSize;
        _playerPrefab = playerPrefab;
        _playerSpawnOffsetY = playerSpawnOffsetY;
        _enemyPrefab = enemyPrefab;
        _enemySpawnOffsetY = enemySpawnOffsetY;
    }

    public void Place(SectionData[] sections, Transform roomParent)
    {
        var normalRoomsWithData = sections
        .Where(s => s.Role != RoomType.Start && s.RoomGridData != null)
        .ToList();

        var enemyTargetSection = normalRoomsWithData.Count > 0
            ? normalRoomsWithData[Random.Range(0, normalRoomsWithData.Count)]
            : null;

        Transform playerTransform = null;

        foreach (var section in sections)
        {
            if (section.RoomGridData == null) continue;
            PlaceRoom(section, roomParent);

            if (section.Role == RoomType.Start)
                playerTransform = PlacePlayer(section, roomParent);
        }

        foreach (var section in sections)
        {
            if (section == enemyTargetSection)
                PlaceEnemy(section, roomParent, playerTransform);
        }
    }

    /// <summary>
    /// セクションの部屋中央に敵を配置する。
    /// </summary>
    private void PlaceEnemy(SectionData section, Transform roomParent, Transform playerTransform)
    {
        if (_enemyPrefab == null) return;

        var worldPos = ResolveRoomCenterWorldPosition(section, _enemySpawnOffsetY);
        var instance = Object.Instantiate(_enemyPrefab, worldPos, Quaternion.identity, roomParent);
        instance.name = $"Enemy_{section.GridPosition}";

        // 生成後にPlayerのTransformを注入する
        if (instance.TryGetComponent<EnemyController>(out var enemy))
            enemy.SetPlayer(playerTransform);
    }

    /// <summary>
    /// 部屋グリッドの中央ワールド座標を返す。
    /// </summary>
    private Vector3 ResolveRoomCenterWorldPosition(SectionData section, float offsetY)
    {
        var gridSize = section.RoomGridData.GridSize;
        return new Vector3(
            (section.RoomGridPosition.x + (gridSize.x - 1) * 0.5f + 0.5f) * _gridSize,
            offsetY,
            (section.RoomGridPosition.y + (gridSize.y - 1) * 0.5f + 0.5f) * _gridSize
        );
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
            // グリッドローカル座標 → ワールド座標
            var localPos = roomData.PlayerPositions[0];
            return new Vector3(
                (section.RoomGridPosition.x + localPos.x + 0.5f) * _gridSize,
                _playerSpawnOffsetY,
                (section.RoomGridPosition.y + localPos.y + 0.5f) * _gridSize
            );
        }

        // PlayerPositions 未設定時は部屋中央にフォールバック
        return ResolveRoomCenterWorldPosition(section, _playerSpawnOffsetY);
    }
}
