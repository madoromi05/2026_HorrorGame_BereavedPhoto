/// <summary>
/// CorridorType‚É‘Î‰ž‚·‚é’Ê˜HPrefab‚ÌDB
/// </summary>
using DungeonSystem;
using UnityEngine;

[CreateAssetMenu(fileName = "CorridorDataBase", menuName = "Dungeon/CorridorDataBase")]
public class CorridorDataBase : ScriptableObject
{
    [System.Serializable]
    public class CorridorEntry
    {
        public CorridorType corridorType;
        public GameObject prefab;
    }

    [SerializeField] private CorridorEntry[] m_entries;

    public GameObject GetPrefab(CorridorType corridorType)
    {
        foreach (var entry in m_entries)
        {
            if (entry.corridorType == corridorType)
                return entry.prefab;
        }

        DebugCustom.LogWarning($"CorridorDataBase: {corridorType}‚É‘Î‰ž‚·‚éPrefab‚ªŒ©‚Â‚©‚è‚Ü‚¹‚ñ");
        return null;
    }
}