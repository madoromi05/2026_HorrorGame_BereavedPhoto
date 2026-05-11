using DungeonSystem;
using UnityEngine;
/// <summary>
/// ランダムダンジョン2Dグリッドのデータ
/// </summary>

[System.Serializable]
public class SectionData
{
    public Vector2Int gridSize;
    public Vector2Int gridPosition;
    public RoomType role;
    public RoomGridData roomGridData;
}