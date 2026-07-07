/// <summary>
/// SectionDataをもとに部屋PrefabとプレイヤーをInstantiateする。
/// 敵の生成はEnemySpawnerに委譲しており、このクラスは部屋配置のみを担う。
/// </summary>
using DungeonSystem;
using HorrorGame.Interaction;
using HorrorGame.Item;
using HorrorGame.UI;
using UnityEngine;
using UnityEngine.AI;

public class SectionPlacer
{
    private const float CellCenterOffset = 0.5f;
    private const float FloorY = 0f;

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
        var prefab = _roomDataBase.GetPrefab(section.Role, section.RoomGridData);
        if (prefab == null) return;


        var worldPos = CellToWorld(section, RoomCenterLocal(section.RoomGridData.GridSize), FloorY);
        var instance = InstantiateAgentsDisabled(prefab, worldPos, roomParent);

        // NavMeshAgent.OnEnable が NavMesh ベイク前に発火してエラーになるのを防ぐため、
        // 非アクティブ状態で Instantiate し、Agent を無効化してからアクティブ化する。
        instance.name = $"Room_{section.Role}_{section.GridPosition}";
    }

    /// <summary>
    /// NavMeshAgent.OnEnable が NavMesh ベイク前に発火してエラーになるのを防ぐため、
    /// 非アクティブ状態で Instantiate し、Agent を無効化してからアクティブ化する。
    /// </summary>
    private static GameObject InstantiateAgentsDisabled(GameObject prefab, Vector3 worldPosition, Transform roomParent)
    {
        bool prefabActive = prefab.activeSelf;
        prefab.SetActive(false);
        var instance = Object.Instantiate(prefab, worldPosition, Quaternion.identity, roomParent);
        prefab.SetActive(prefabActive);
        foreach (var agent in instance.GetComponentsInChildren<NavMeshAgent>(true))
            agent.enabled = false;

        instance.SetActive(true);
        return instance;
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

    // 部屋ローカルのセル座標をワールド座標へ変換する（座標規約はここに一本化）
    private Vector3 CellToWorld(SectionData section, Vector2 localCell, float y)
    {
        return new Vector3(
            (section.RoomGridPosition.x + localCell.x + CellCenterOffset) * _gridSize,
            y,
            (section.RoomGridPosition.y + localCell.y + CellCenterOffset) * _gridSize);
    }

    // 部屋中央のローカルセル座標（偶数サイズの場合はセル境界）を返す。
    private static Vector2 RoomCenterLocal(Vector2Int gridSize)
        => new Vector2((gridSize.x - 1) * 0.5f, (gridSize.y - 1) * 0.5f);
}