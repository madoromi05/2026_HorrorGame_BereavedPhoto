/// <summary>
/// CorridorTypeに対応する通路PrefabのDB
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
        public CorridorType CorridorType;   // 通路の形状種別
        public GameObject CorridorPrefab;   // 対応する通路のPrefab
    }

    // 通路形状とPrefabの対応リスト
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

        DebugCustom.LogWarning($"CorridorDataBase: {corridorType} に対応する Prefab が見つかりません");
        return null;
    }
}
