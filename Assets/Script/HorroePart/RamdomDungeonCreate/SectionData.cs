using DungeonSystem;
using UnityEngine;

/// <summary>
/// ランダムダンジョン生成用2Dグリッドのデータ
/// </summary>
[System.Serializable]
public class SectionData
{
    public Vector2Int GridSize;
    public Vector2Int GridPosition;
    public Vector2Int RoomGridPosition; // 部屋の左上グリッド座標（セクション座標 + ランダムオフセット）
    public RoomType Role;
    public RoomGridData RoomGridData;
}
