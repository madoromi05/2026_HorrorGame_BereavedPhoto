/// <summary>
/// RoomDataBaseの設定に従い、各セクションに敵を配置する。
/// SectionPlacerから敵生成責務を分離したクラス。
/// PlayerのTransformはPlace呼び出し時に渡すことで、
/// 部屋配置とプレイヤー生成の完了を待ってから敵を生成できる。
/// </summary>
using DungeonSystem;
using UnityEngine;

public class EnemySpawner
{
    private readonly RoomDataBase _roomDataBase;
    private readonly float _gridSize;
    private readonly float _enemySpawnOffsetY;

    public EnemySpawner(RoomDataBase roomDataBase, float gridSize, float enemySpawnOffsetY)
    {
        _roomDataBase = roomDataBase;
        _gridSize = gridSize;
        _enemySpawnOffsetY = enemySpawnOffsetY;
    }

    /// <summary>
    /// 全セクションを走査し、RoomDataBaseの設定に従って敵を生成する。
    /// playerTransformがnullの場合は追跡なしで生成する。
    /// gridはMapWandererのウェイポイント構築に使用する。
    /// </summary>
    public void Place(SectionData[] sections, Transform enemyParent, Transform playerTransform, GridType[,] grid)
    {
        foreach (var section in sections)
        {
            if (section.RoomGridData == null) continue;

            var enemyEntries = _roomDataBase.GetEnemyEntries(section.Role);
            foreach (var entry in enemyEntries)
            {
                if (entry.EnemyPrefab == null) continue;
                SpawnEnemies(section, entry, enemyParent, playerTransform, grid);
            }
        }
    }

    private void SpawnEnemies(
        SectionData section,
        RoomDataBase.EnemyEntry entry,
        Transform enemyParent,
        Transform playerTransform,
        GridType[,] grid)
    {
        var roomBounds = CalcRoomBounds(section);

        for (int i = 0; i < entry.SpawnCount; i++)
        {
            var worldPos = CalcRoomCenterWorldPosition(section);
            var instance = Object.Instantiate(entry.EnemyPrefab, worldPos, Quaternion.identity, enemyParent);
            instance.name = $"Enemy_{section.Role}_{section.GridPosition}_{i}";

            if (instance.TryGetComponent<EnemyController>(out var controller))
                controller.SetPlayer(playerTransform);

            // 部屋内徘徊型には生成部屋のAABBを注入する
            if (instance.TryGetComponent<RoomWanderer>(out var roomWanderer))
                roomWanderer.SetRoomBounds(roomBounds);

            // マップ全体徘徊型にはグリッド情報を注入して通路ウェイポイントを構築させる
            if (instance.TryGetComponent<MapWanderer>(out var mapWanderer))
                mapWanderer.SetGrid(grid, _gridSize);
        }
    }

    /// <summary>
    /// セクションのRoomGridDataのDoor外縁をもとに部屋のAABBを算出する。
    /// Doorが未設定の場合はRoomGridSizeをそのまま使う。
    /// </summary>
    private Bounds CalcRoomBounds(SectionData section)
    {
        var roomData = section.RoomGridData;
        var origin = section.RoomGridPosition;

        // Door座標がある場合はDoorの内側ギリギリをAABBとして使用する
        // （Doorを踏まずに引き返すことで「部屋から出ない」を実現する）
        if (roomData.DoorPositions != null && roomData.DoorPositions.Count > 0)
        {
            var min = new Vector2Int(int.MaxValue, int.MaxValue);
            var max = new Vector2Int(int.MinValue, int.MinValue);

            foreach (var door in roomData.DoorPositions)
            {
                if (door.x < min.x) min.x = door.x;
                if (door.y < min.y) min.y = door.y;
                if (door.x > max.x) max.x = door.x;
                if (door.y > max.y) max.y = door.y;
            }

            var worldMin = new Vector3(
                (origin.x + min.x) * _gridSize,
                -10f,
                (origin.y + min.y) * _gridSize
            );
            var worldMax = new Vector3(
                (origin.x + max.x + 1) * _gridSize,
                10f,
                (origin.y + max.y + 1) * _gridSize
            );
            var bounds = new Bounds();
            bounds.SetMinMax(worldMin, worldMax);
            return bounds;
        }

        // Doorなし：RoomGridSizeをAABBとして使用
        var fallbackMin = new Vector3(origin.x * _gridSize, -10f, origin.y * _gridSize);
        var fallbackMax = new Vector3(
            (origin.x + roomData.GridSize.x) * _gridSize,
            10f,
            (origin.y + roomData.GridSize.y) * _gridSize
        );
        var fallback = new Bounds();
        fallback.SetMinMax(fallbackMin, fallbackMax);
        return fallback;
    }

    private Vector3 CalcRoomCenterWorldPosition(SectionData section)
    {
        var gridSize = section.RoomGridData.GridSize;
        return new Vector3(
            (section.RoomGridPosition.x + (gridSize.x - 1) * 0.5f + 0.5f) * _gridSize,
            _enemySpawnOffsetY,
            (section.RoomGridPosition.y + (gridSize.y - 1) * 0.5f + 0.5f) * _gridSize
        );
    }
}