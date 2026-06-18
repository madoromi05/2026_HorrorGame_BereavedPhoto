/// <summary>
/// RoomTypeに対応する部屋PrefabのDB
/// </summary>
using DungeonSystem;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomDataBase", menuName = "Dungeon/RoomDataBase")]
public class RoomDataBase : ScriptableObject
{
    [System.Serializable]
    public class EnemyEntry
    {
        public GameObject EnemyPrefab;
        public int SpawnCount = 1;
    }

    [System.Serializable]
    public class RoomEntry
    {
        public RoomType RoomType;

        public GameObject Prefab;
        public RoomGridData[] RoomGridDatas;
        public EnemyEntry[] EnemyEntries;
    }

    [SerializeField] private RoomEntry[] _entries;
    private Dictionary<RoomType, RoomEntry> _entryMap;

    private void OnEnable()
    {
        // キャッシュの構築
        _entryMap = new Dictionary<RoomType, RoomEntry>(_entries.Length);
        foreach (var entry in _entries)
            _entryMap[entry.RoomType] = entry;
    }

    public GameObject GetPrefab(RoomType roomType)
    {
        if (_entryMap.TryGetValue(roomType, out var entry))
            return entry.Prefab;

        DebugCustom.LogWarning($"RoomDataBase: {roomType}に対応するPrefabが見つかりません");
        return null;
    }

    /// <summary>
    /// 指定RoomTypeのRoomGridDataをランダムに返す。
    /// RoomGridDatasが空の場合はnullを返す。
    /// </summary>
    public RoomGridData GetRoomGridData(RoomType roomType, int index)
    {
        if (!_entryMap.TryGetValue(roomType, out var entry))
        {
            DebugCustom.LogWarning($"RoomDataBase: {roomType} に対応する RoomGridData が見つかりません");
            return null;
        }
        if (entry.RoomGridDatas.Length == 0) return null;
        return entry.RoomGridDatas[index % entry.RoomGridDatas.Length];
    }

    /// <summary>
    /// 指定RoomTypeに登録されているRoomGridDataの数を返す。
    /// 登録されていない場合は0を返す。
    /// </summary>
    public int GetRoomGridDataCount(RoomType roomType)
    {
        if (_entryMap.TryGetValue(roomType, out var entry))
            return entry.RoomGridDatas?.Length ?? 0;
        return 0;
    }

    /// <summary>
    /// 指定RoomTypeの敵エントリ一覧を返す。未設定の場合は空配列を返す。
    /// </summary>
    public EnemyEntry[] GetEnemyEntries(RoomType roomType)
    {
        if (_entryMap.TryGetValue(roomType, out var entry))
            return entry.EnemyEntries ?? System.Array.Empty<EnemyEntry>();

        return System.Array.Empty<EnemyEntry>();
    }
}