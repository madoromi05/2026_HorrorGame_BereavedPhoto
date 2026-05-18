/// <summary>
/// RoomTypeに対応する部屋PrefabのDB
/// </summary>
using DungeonSystem;
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

        // この部屋タイプを何部屋まで生成するか（0 = 上限なし）
        public int MaxCount;

        public GameObject Prefab;
        public RoomGridData[] RoomGridDatas;
        public EnemyEntry[] EnemyEntries;
    }

    //RoomTypeごとに「Prefab」と「RoomGridDataの候補リスト」をセットで登録する
    [SerializeField] private RoomEntry[] _entries;

    public GameObject GetPrefab(RoomType roomType)
    {
        foreach (var entry in _entries)
        {
            if (entry.RoomType == roomType)
                return entry.Prefab;
        }

        DebugCustom.LogWarning($"RoomDataBase: {roomType}に対応するPrefabが見つかりません");
        return null;
    }

    /// <summary>
    /// 指定RoomTypeのRoomGridDataをランダムに返す。
    /// RoomGridDatasが空の場合はnullを返す。
    /// </summary>
    public RoomGridData GetRandomRoomGridData(RoomType roomType)
    {
        foreach (var entry in _entries)
        {
            if (entry.RoomType != roomType) continue;
            if (entry.RoomGridDatas.Length == 0) return null;
            return entry.RoomGridDatas[Random.Range(0, entry.RoomGridDatas.Length)];
        }

        DebugCustom.LogWarning($"RoomDataBase: {roomType}に対応するRoomGridDataが見つかりません");
        return null;
    }

    /// <summary>
    /// 指定RoomTypeの最大生成数を返す。0は上限なしを意味する。
    /// </summary>
    public int GetMaxCount(RoomType roomType)
    {
        foreach (var entry in _entries)
        {
            if (entry.RoomType == roomType)
                return entry.MaxCount;
        }
        return 0;
    }

    /// <summary>
    /// 指定RoomTypeの敵エントリ一覧を返す。未設定の場合は空配列を返す。
    /// </summary>
    public EnemyEntry[] GetEnemyEntries(RoomType roomType)
    {
        foreach (var entry in _entries)
        {
            if (entry.RoomType == roomType)
                return entry.EnemyEntries ?? System.Array.Empty<EnemyEntry>();
        }
        return System.Array.Empty<EnemyEntry>();
    }
}