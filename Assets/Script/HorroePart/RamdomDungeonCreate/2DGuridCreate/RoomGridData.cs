using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 部屋のグリッドデータ。
/// GridSize内の各セルはInspectorのGUIで Floor / Door / Wall を設定する。
/// Floor = 通行可能な室内セル
/// Door  = 壁の開口部（通路との接続点）
/// Wall  = 通行不可の壁セル（FBXの壁メッシュと一致させること）
///
/// EnemyNavWallCells は EnemyNavSubdivision で細分化したナビグリッド上の
/// 移動不可セル位置（部屋ローカル座標）。FieldBluePrint.EnemyNavSubdivision と
/// 値を合わせておくこと。
/// </summary>
[CreateAssetMenu(fileName = "RoomGridData", menuName = "Dungeon/RoomGridData")]
public class RoomGridData : ScriptableObject
{
    public Vector2Int GridSize;
    public List<Vector2Int> DoorPositions   = new List<Vector2Int>();
    public List<Vector2Int> WallPositions   = new List<Vector2Int>();
    public List<Vector2Int> PlayerPositions = new List<Vector2Int>();

    [Header("Enemy Navigation")]
    // FieldBluePrint.EnemyNavSubdivision と合わせること（エディタ表示・保存に使用）
    [Range(1, 10)] public int EnemyNavSubdivision = 5;
    // 敵移動不可ナビセルの位置（部屋ローカルナビ座標、0〜GridSize*EnemyNavSubdivision-1）
    public List<Vector2Int> EnemyNavWallCells = new List<Vector2Int>();

    [Header("Auto-fill (Editor Only)")]
    // Auto-fillボタンでコライダーを走査するためのプレハブ参照（エディタ専用）
    public GameObject RoomPrefab;
}
