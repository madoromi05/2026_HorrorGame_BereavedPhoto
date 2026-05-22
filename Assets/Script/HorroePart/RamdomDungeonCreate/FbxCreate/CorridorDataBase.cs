/// <summary>
/// CorridorType‚É‘Î‰‚·‚é’Ê˜HPrefab‚ÌDB
/// </summary>
using DungeonSystem;
using System.Collections.Generic;
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
    private Dictionary<CorridorType, GameObject> _prefabMap;

    private void OnEnable()
    {
        _prefabMap = new Dictionary<CorridorType, GameObject>(_entries.Length);
        foreach (var entry in _entries)
        {
            _prefabMap[entry.CorridorType] = entry.CorridorPrefab;
        }
    }

    public GameObject GetPrefab(CorridorType corridorType)
    {
        if (_prefabMap.TryGetValue(corridorType, out var prefab))
            return prefab;

        DebugCustom.LogWarning($"CorridorDataBase: {corridorType} ‚É‘Î‰‚·‚é Prefab ‚ªŒ©‚Â‚©‚è‚Ü‚¹‚ñ");
        return null;
    }
}
