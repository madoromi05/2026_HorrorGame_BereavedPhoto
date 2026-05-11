/// <summary>
/// SectionDataを元に部屋PrefabをInstantiateする
/// 親オブジェクトはDungeonGeneratorから受け取る
/// </summary>

using DungeonSystem;
using UnityEngine;

public class SectionPlacer
{
    private RoomDataBase m_roomDataBase;
    private float m_gridSize;

    public SectionPlacer(RoomDataBase roomDataBase, float gridSize)
    {
        m_roomDataBase = roomDataBase;
        m_gridSize = gridSize;
    }

    /// <summary>
    /// 全Sectionを走査し部屋PrefabをInstantiateする
    /// roomGridDataがnull、Section（通路中継点）の場合はスキップする
    /// </summary>
    public void Place(SectionData[] sections, Transform roomParent)
    {
        foreach (var section in sections)
        {
            if (section.roomGridData == null) continue;
            PlaceRoom(section, roomParent);
        }
    }
    /// <summary>
    /// SectionのRoomTypeに対応するPrefabをワールド座標に配置する
    /// </summary>
    private void PlaceRoom(SectionData section, Transform roomParent)
    {
        var prefab = m_roomDataBase.GetPrefab(section.role);
        if (prefab == null) return;

        // グリッド座標をUnityワールド座標に変換
        var worldPos = new Vector3(
            section.gridPosition.x * m_gridSize,
            0f,
            section.gridPosition.y * m_gridSize
        );

        var instance = Object.Instantiate(prefab, worldPos, Quaternion.identity, roomParent);
        instance.name = $"Room_{section.role}_{section.gridPosition}";
    }
}