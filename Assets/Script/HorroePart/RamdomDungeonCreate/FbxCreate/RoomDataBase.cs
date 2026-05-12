/// <summary>
/// RoomTypeに対応する部屋PrefabのDB
/// </summary>
using DungeonSystem;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomDataBase", menuName = "Dungeon/RoomDataBase")]
public class RoomDataBase : ScriptableObject
{
    [System.Serializable]
    public class RoomEntry
    {
        public RoomType RoomType;               // 部屋の種類
        public GameObject Prefab;               // 部屋Prefab
        public RoomGridData[] RoomGridDatas;    // 部屋のグリッドデータ
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
    /// 指定RoomTypeのRoomGridDataをランダムに返す
    /// RoomGridDatasが空の場合はnullを返す
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
}