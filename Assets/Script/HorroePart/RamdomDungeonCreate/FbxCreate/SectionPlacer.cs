/// <summary>
/// SectionDataをもとに部屋PrefabをInstantiateする
/// Start ルームを置いたら、プレイヤーの移動を PlayerStartPositioner に任せる。敵の生成は EnemySpawner が行う
/// </summary>
using DungeonSystem;
using HorrorGame.Interaction;
using HorrorGame.Item;
using HorrorGame.UI;
using UnityEngine;
using UnityEngine.AI;

public class SectionPlacer
{
    private const float kFloorY = 0f;	// 部屋を置く床の高さ

    private readonly RoomDataBase _roomDataBase;
    private readonly float _gridSize;
    private readonly PlayerStartPositioner _playerPositioner;
    public SectionPlacer(
        RoomDataBase roomDataBase,
        float gridSize,
        Transform player)
    {
        _roomDataBase = roomDataBase;
        _gridSize = gridSize;
        _playerPositioner = new PlayerStartPositioner(player);
    }

    /// <summary>
    /// 全セクションに部屋とプレイヤーを配置する
    /// 戻り値のTransformはEnemySpawnerへの注入に使用するため、NavMeshベイク後に渡すこと
    /// </summary>
    public Transform Place(SectionData[] sections, Transform roomParent)
    {
        Transform playerTransform = null;

        foreach (var section in sections)
        {
            if (section.RoomGridData == null) continue;

            var instance = PlaceRoom(section, roomParent);

            if (section.Role == RoomType.Start)
                playerTransform = _playerPositioner.MoveToStart(instance);
        }

        return playerTransform;
    }

    private GameObject PlaceRoom(SectionData section, Transform roomParent)
    {
        var prefab = _roomDataBase.GetPrefab(section.Role, section.RoomGridData);
        if (prefab == null) return null;

        var worldPos = GridUtil.CellCenterToWorld(
            section.RoomGridPosition + GridUtil.RoomCenterCell(section.RoomGridData.GridSize), _gridSize, kFloorY);
        var instance = InstantiateAgentsDisabled(prefab, worldPos, roomParent);

        instance.name = $"Room_{section.Role}_{section.GridPosition}";
        return instance;
    }

    /// <summary>
    /// NavMeshAgent.OnEnable が NavMesh ベイク前に発火してエラーになるのを防ぐため、
    /// 非アクティブ状態で Instantiate し、Agent を無効化してからアクティブ化する
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
}