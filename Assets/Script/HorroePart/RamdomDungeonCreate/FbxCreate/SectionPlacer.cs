/// <summary>
/// SectionDataをもとに部屋PrefabとプレイヤーをInstantiateする。
/// 敵の生成はEnemySpawnerに委譲しており、このクラスは部屋配置のみを担う。
/// </summary>
using DungeonSystem;
using HorrorGame.Interaction;
using HorrorGame.Item;
using HorrorGame.UI;
using UnityEngine;

public class SectionPlacer
{
    private readonly RoomDataBase _roomDataBase;
    private readonly float _gridSize;
    private readonly Transform _player;
    private readonly float _playerSpawnOffsetY;
    public SectionPlacer(
        RoomDataBase roomDataBase,
        float gridSize,
        Transform player,
        float playerSpawnOffsetY)
    {
        _roomDataBase = roomDataBase;
        _gridSize = gridSize;
        _player = player;
        _playerSpawnOffsetY = playerSpawnOffsetY;
    }

    /// <summary>
    /// 全セクションに部屋とプレイヤーを配置する。
    /// 戻り値のTransformはEnemySpawnerへの注入に使用するため、NavMeshベイク後に渡すこと。
    /// </summary>
    public Transform Place(SectionData[] sections, Transform roomParent)
    {
        Transform playerTransform = null;

        foreach (var section in sections)
        {
            if (section.RoomGridData == null) continue;

            PlaceRoom(section, roomParent);

            if (section.Role == RoomType.Start)
                playerTransform = PlayerTransform(section);
        }

        return playerTransform;
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
    /// StartセクションのRoomGridDataに登録されたPlayerPositionsの先頭セルにプレイヤーを移動させる。
    /// PlayerPositionsが未設定の場合は部屋中央にフォールバックする。
    /// </summary>
    private Transform PlayerTransform(SectionData section)
    {
        var pos = ResolvePlayerWorldPosition(section);

        if (_player.TryGetComponent<CharacterController>(out var cc))
            cc.enabled = false;

        _player.position = pos;

        if (cc != null)
            cc.enabled = true;

        return _player;
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