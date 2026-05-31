/// <summary>
/// RoomDataBaseの設定に従い、各セクションに敵を配置する。
/// SectionPlacerから敵生成責務を分離したクラス。
/// PlayerのTransformはPlace呼び出し時に渡すことで、
/// 部屋配置とプレイヤー生成の完了を待ってから敵を生成できる。
///
/// Place() 内でダンジョングリッドとセクション情報から DungeonPathfinder を1つ生成し、
/// 全ての敵インスタンスに共有することでメモリと計算コストを節約する。
/// </summary>
using DungeonSystem;
using UnityEngine;

public class EnemySpawner
{
    private readonly RoomDataBase _roomDataBase;
    private readonly float _gridSize;
    private readonly float _enemySpawnOffsetY;
    private readonly int _navSubdivision;

    public EnemySpawner(RoomDataBase roomDataBase, float gridSize, float enemySpawnOffsetY, int navSubdivision)
    {
        _roomDataBase     = roomDataBase;
        _gridSize         = gridSize;
        _enemySpawnOffsetY = enemySpawnOffsetY;
        _navSubdivision   = navSubdivision;
    }

    /// <summary>
    /// 全セクションを走査し、RoomDataBaseの設定に従って敵を生成する。
    /// playerTransformがnullの場合は追跡なしで生成する。
    /// </summary>
    public void Place(SectionData[] sections, Transform enemyParent, Transform playerTransform, GridType[,] grid, bool enemyLookDebug = false)
    {
        // ナビグリッドとパスファインダーをここで1回だけ構築し全敵に共有する
        var walkable    = EnemyNavGridBuilder.Build(grid, _gridSize, _navSubdivision, sections);
        float navCellSize = EnemyNavGridBuilder.GetNavCellSize(_gridSize, _navSubdivision);
        var pathfinder  = new DungeonPathfinder();
        pathfinder.SetNavGrid(walkable, navCellSize);

        foreach (var section in sections)
        {
            if (section.RoomGridData == null) continue;

            var enemyEntries = _roomDataBase.GetEnemyEntries(section.Role);
            foreach (var entry in enemyEntries)
            {
                if (entry.EnemyPrefab == null) continue;
                SpawnEnemies(section, entry, enemyParent, playerTransform, grid, pathfinder, enemyLookDebug);
            }
        }
    }

    private void SpawnEnemies(
        SectionData section,
        RoomDataBase.EnemyEntry entry,
        Transform enemyParent,
        Transform playerTransform,
        GridType[,] grid,
        DungeonPathfinder pathfinder,
        bool enemyLookDebug)
    {
        var roomBounds  = CalcRoomBounds(section);
        var roomCenter  = CalcRoomCenterWorldPosition(section);

        for (int i = 0; i < entry.SpawnCount; i++)
        {
            var worldPos = roomCenter;
            if (entry.SpawnCount > 1)
            {
                var offset = Random.insideUnitCircle * _gridSize * 0.5f;
                worldPos += new Vector3(offset.x, 0f, offset.y);
            }

            var instance = Object.Instantiate(entry.EnemyPrefab, worldPos, Quaternion.identity, enemyParent);
            instance.name = $"Enemy_{section.Role}_{section.GridPosition}_{i}";

            if (instance.TryGetComponent<EnemyController>(out var controller))
            {
                controller.SetPlayer(playerTransform);
                controller.SetPathfinder(pathfinder);
            }

            if (instance.TryGetComponent<RoomWanderer>(out var roomWanderer))
                roomWanderer.SetRoomBounds(roomBounds);

            if (instance.TryGetComponent<MapWanderer>(out var mapWanderer))
                mapWanderer.SetGrid(grid, _gridSize, pathfinder);
        }
    }

    private Bounds CalcRoomBounds(SectionData section)
    {
        var roomData = section.RoomGridData;
        var origin   = section.RoomGridPosition;

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

            var worldMin = new Vector3((origin.x + min.x) * _gridSize, -10f, (origin.y + min.y) * _gridSize);
            var worldMax = new Vector3((origin.x + max.x + 1) * _gridSize, 10f, (origin.y + max.y + 1) * _gridSize);
            var bounds   = new Bounds();
            bounds.SetMinMax(worldMin, worldMax);
            return bounds;
        }

        var fallbackMin = new Vector3(origin.x * _gridSize, -10f, origin.y * _gridSize);
        var fallbackMax = new Vector3(
            (origin.x + roomData.GridSize.x) * _gridSize, 10f,
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
