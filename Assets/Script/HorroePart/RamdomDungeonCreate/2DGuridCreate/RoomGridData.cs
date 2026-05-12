using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 部屋のグリッドデータ。
/// GridSize内の各セルはInspectorのGUIで Floor / Door / Wall を設定する。
/// Floor = 通行可能な室内セル
/// Door  = 壁の開口部（通路との接続点）
/// Wall  = 通行不可の壁セル（FBXの壁メッシュと一致させること）
/// </summary>
[CreateAssetMenu(fileName = "RoomGridData", menuName = "Dungeon/RoomGridData")]
public class RoomGridData : ScriptableObject
{
    public Vector2Int GridSize;
    public List<Vector2Int> DoorPositions  = new List<Vector2Int>();
    public List<Vector2Int> WallPositions  = new List<Vector2Int>();
}
