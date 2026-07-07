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
    public SectionPlacer(
        RoomDataBase roomDataBase,
        float gridSize,
        Transform player)
    {
        _roomDataBase = roomDataBase;
        _gridSize = gridSize;
        _player = player;
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

            var instance = PlaceRoom(section, roomParent);

            if (section.Role == RoomType.Start)
                playerTransform = PlayerTransform(instance);
        }

        return playerTransform;
    }

    private GameObject PlaceRoom(SectionData section, Transform roomParent)
    {
        var prefab = _roomDataBase.GetPrefab(section.Role, section.RoomGridData);
        if (prefab == null) return null;


        var worldPos = CellToWorld(section, RoomCenterLocal(section.RoomGridData.GridSize), FloorY);
        var instance = InstantiateAgentsDisabled(prefab, worldPos, roomParent);

        // NavMeshAgent.OnEnable が NavMesh ベイク前に発火してエラーになるのを防ぐため、
        // 非アクティブ状態で Instantiate し、Agent を無効化してからアクティブ化する。
        instance.name = $"Room_{section.Role}_{section.GridPosition}";
        return instance;
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
    /// プレイヤーをStartルームの初期位置へ移動させる。
    /// StartルームPrefab内のPlayerSpawnPointマーカーの位置・向きをそのまま使用する。
    /// マーカーが無い場合はスポーン位置を決められないため、警告のみ出して現在位置を維持する
    /// （部屋中央などへのフォールバックは行わない）。
    /// </summary>
    private Transform PlayerTransform(GameObject startRoomInstance)
    {
        var marker = startRoomInstance != null
            ? startRoomInstance.GetComponentInChildren<PlayerSpawnPoint>(true)
            : null;

        if (marker == null)
        {
            Debug.LogWarning(
                "SectionPlacer: StartルームにPlayerSpawnPointマーカーが見つかりません。" +
                "StartルームのPrefabにPlayerSpawnPointを配置してください。" +
                "プレイヤーの位置は変更しません。");
            return _player;
        }

        if (_player.TryGetComponent<CharacterController>(out var cc))
            cc.enabled = false;

        _player.position = marker.Position;

        // PlayerMover は内部Yawを基準に回転を制御するため、直接rotationを書くだけでは
        // 最初の視点入力で元に戻ってしまう。SetYaw経由で内部状態も同期させる。
        if (_player.TryGetComponent<PlayerMover>(out var mover))
            mover.SetYaw(marker.Yaw);
        else
            _player.rotation = Quaternion.Euler(0f, marker.Yaw, 0f);

        if (cc != null)
            cc.enabled = true;

        return _player;
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