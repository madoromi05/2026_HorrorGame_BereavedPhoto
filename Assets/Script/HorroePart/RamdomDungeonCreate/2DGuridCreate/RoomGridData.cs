using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 部屋一個のグリッドのデータ
/// </summary>


[CreateAssetMenu(fileName = "RoomGridData", menuName = "Dungeon/RoomGridData")]
public class RoomGridData : ScriptableObject
{
    public Vector2Int size;                  // 部屋が占有するグリッドサイズ
    public List<Vector2Int> doorPositions;   // 部屋内のDoorセル位置
}
