/// <summary>
/// RoomDataBase の設定に従い、各セクションに敵を配置する。
/// SectionPlacer から敵生成責務を分離したクラス。
/// プレイヤー参照の付与は EnemyPlayerLinker が OnRoomPlaced 経由で行う。
/// </summary>
using DungeonSystem;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner
{
    private readonly RoomDataBase _roomDataBase;
    private readonly float _gridSize;
    private readonly float _enemySpawnOffsetY;

    public EnemySpawner(RoomDataBase roomDataBase, float gridSize, float enemySpawnOffsetY)
    {
        _roomDataBase      = roomDataBase;
        _gridSize          = gridSize;
        _enemySpawnOffsetY = enemySpawnOffsetY;
    }

    public void Place(SectionData[] sections, Transform enemyParent)
    {
        foreach (var section in sections)
        {
            if (section.RoomGridData == null) continue;

            var enemyEntries = _roomDataBase.GetEnemyEntries(section.Role);
            foreach (var entry in enemyEntries)
            {
                if (entry.EnemyPrefab == null) continue;
                SpawnEnemies(section, entry, enemyParent);
            }
        }
    }

    private void SpawnEnemies(
        SectionData section,
        RoomDataBase.EnemyEntry entry,
        Transform enemyParent)
    {
        var roomCenter = CalcRoomCenterWorldPosition(section);

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

            // NavMeshAgent の baseOffset で Y 高さを制御する（Rigidbody の FreezePositionY の代替）
            if (instance.TryGetComponent<NavMeshAgent>(out var agent))
                agent.baseOffset = _enemySpawnOffsetY;
        }
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
