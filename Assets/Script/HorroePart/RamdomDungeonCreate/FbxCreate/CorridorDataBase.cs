/// <summary>
/// CorridorType‚É‘Î‰‚·‚é’Ê˜HPrefab‚ÌDB
/// </summary>
using DungeonSystem;
using UnityEngine;

[CreateAssetMenu(fileName = "CorridorDataBase", menuName = "Dungeon/CorridorDataBase")]
public class CorridorDataBase : ScriptableObject
{
    [System.Serializable]
    public class CorridorEntry
    {
        public CorridorType CorridorType;   // ’Ê˜H‚ÌŒ`óí•Ê
        public GameObject CorridorPrefab;   // ‘Î‰‚·‚é’Ê˜H‚ÌPrefab
    }

    // ’Ê˜HŒ`ó‚ÆPrefab‚Ì‘Î‰ƒŠƒXƒg
    [SerializeField] private CorridorEntry[] _entries;
    public GameObject GetPrefab(CorridorType corridorType)
    {
        foreach (var entry in _entries)
        {
            if (entry.CorridorType == corridorType)
                return entry.CorridorPrefab;
        }

        DebugCustom.LogWarning($"CorridorDataBase: {corridorType}‚É‘Î‰‚·‚éPrefab‚ªŒ©‚Â‚©‚è‚Ü‚¹‚ñ");
        return null;
    }
}