/// <summary>
/// RoomType‚É‘Î‰ž‚·‚é•”‰®Prefab‚ÌDB
/// </summary>
using DungeonSystem;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomDataBase", menuName = "Dungeon/RoomDataBase")]
public class RoomDataBase : ScriptableObject
{
    [System.Serializable]
    public class RoomEntry
    {
        public RoomType roomType;
        public GameObject prefab;
    }

    [SerializeField] private RoomEntry[] m_entries;

    public GameObject GetPrefab(RoomType roomType)
    {
        foreach (var entry in m_entries)
        {
            if (entry.roomType == roomType)
                return entry.prefab;
        }

        DebugCustom.LogWarning($"RoomDataBase: {roomType}‚É‘Î‰ž‚·‚éPrefab‚ªŒ©‚Â‚©‚è‚Ü‚¹‚ñ");
        return null;
    }
}