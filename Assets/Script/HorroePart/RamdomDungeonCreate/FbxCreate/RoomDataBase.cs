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
    // シリアライズフィールドを汚染しないよう、結合済みRoomGridDatasはランタイム専用で保持
    private Dictionary<RoomType, RoomGridData[]> _mergedRoomGridDatas;
    // 同一RoomTypeに複数エントリ（＝物理サイズの異なるPrefab違い）がある場合、
    // RoomGridDataごとに「本来対応するPrefab」を引けるようにするための対応表。
    // これが無いと GetPrefab(RoomType) が常に最初のエントリのPrefabを返し、
    // ランダムに選ばれたRoomGridData（例: 32x32用）と実際にInstantiateされるPrefab（例: 40x40）が
    // 食い違うバグになる。
    private Dictionary<RoomGridData, GameObject> _roomGridDataToPrefab;

    private void OnEnable()
    {
        _entryMap = new Dictionary<RoomType, RoomEntry>(_entries.Length);
        var roomGridDataAccum = new Dictionary<RoomType, List<RoomGridData>>();
        _roomGridDataToPrefab = new Dictionary<RoomGridData, GameObject>();

        foreach (var entry in _entries)
        {
            if (!_entryMap.ContainsKey(entry.RoomType))
            {
                _entryMap[entry.RoomType] = entry;
                roomGridDataAccum[entry.RoomType] = new List<RoomGridData>();
            }

            // 同一RoomTypeは RoomGridDatas を結合する（EnemyEntries は最初のエントリを使用）が、
            // Prefab は各エントリ自身のものを RoomGridData ごとに紐付けて記録する。
            if (entry.RoomGridDatas != null)
            {
                foreach (var d in entry.RoomGridDatas)
                {
                    if (d == null) continue;
                    roomGridDataAccum[entry.RoomType].Add(d);
                    _roomGridDataToPrefab[d] = entry.Prefab;
                }
            }
        }

        // 結合結果はランタイム専用辞書に保存し、シリアライズフィールドは書き換えない
        _mergedRoomGridDatas = new Dictionary<RoomType, RoomGridData[]>(roomGridDataAccum.Count);
        foreach (var kv in roomGridDataAccum)
            _mergedRoomGridDatas[kv.Key] = kv.Value.ToArray();
    }

    public GameObject GetPrefab(RoomType roomType)
    {
        if (_entryMap.TryGetValue(roomType, out var entry))
            return entry.Prefab;

        DebugCustom.LogWarning($"RoomDataBase: {roomType}に対応するPrefabが見つかりません");
        return null;
    }

    /// <summary>
    /// 指定のRoomGridDataが本来対応しているPrefabを返す。
    /// 同一RoomTypeに複数のPrefabバリエーションが登録されている場合でも、
    /// RoomGridDataを実際に生成したエントリのPrefabを正しく取得できる。
    /// 対応が見つからない場合は roomType の代表Prefab（先頭エントリ）にフォールバックする。
    /// </summary>
    public GameObject GetPrefab(RoomType roomType, RoomGridData roomGridData)
    {
        if (roomGridData != null && _roomGridDataToPrefab.TryGetValue(roomGridData, out var prefab))
            return prefab;

        return GetPrefab(roomType);
    }

    /// <summary>
    /// 指定RoomTypeのRoomGridDataをランダムに返す。
    /// RoomGridDatasが空の場合はnullを返す。
    /// </summary>
    public RoomGridData GetRoomGridData(RoomType roomType, int index)
    {
        if (!_mergedRoomGridDatas.TryGetValue(roomType, out var datas))
        {
            DebugCustom.LogWarning($"RoomDataBase: {roomType} に対応する RoomGridData が見つかりません");
            return null;
        }
        if (datas.Length == 0) return null;
        return datas[index % datas.Length];
    }

    /// <summary>
    /// 指定RoomTypeに登録されているRoomGridDataの数を返す。
    /// 登録されていない場合は0を返す。
    /// </summary>
    public int GetRoomGridDataCount(RoomType roomType)
    {
        if (_mergedRoomGridDatas.TryGetValue(roomType, out var datas))
            return datas.Length;
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