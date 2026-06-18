using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1 部屋分のグリッドデータ。Inspector で各セルに Floor / Door / Wall / Player を割り当て、
/// 敵ナビゲーション用のブロックセルリストと Auto-fill パラメータを保持する。
/// セルはリストへの有無で種別を表現しており、Floor はいずれにも含まれない状態を意味する。
/// </summary>
[CreateAssetMenu(fileName = "RoomGridData", menuName = "Dungeon/RoomGridData")]
public class RoomGridData : ScriptableObject
{
    public Vector2Int GridSize;
    public List<Vector2Int> DoorPositions   = new List<Vector2Int>();
    public List<Vector2Int> WallPositions   = new List<Vector2Int>();
    public List<Vector2Int> PlayerPositions = new List<Vector2Int>();

    [Header("Enemy Navigation")]
    // 変更時は EnemyNavWallCells の内容が無効になるためエディタ側でクリアされる。
    [Range(1, 10)] public int EnemyNavSubdivision = 5;

    // ナビセル座標は (0, 0) 〜 (GridSize * EnemyNavSubdivision - 1) の範囲。
    public List<Vector2Int> EnemyNavWallCells = new List<Vector2Int>();

    [Header("Auto-fill (Editor Only)")]
    public GameObject RoomPrefab;

    [Header("Auto-fill 検知ボックス設定")]
    // 床面（Y=0）や低い障害物を誤検知しないよう、下限は床より少し上に設定する。
    [Tooltip("コライダー検知ボックスの Y 最小値")]
    public float NavCheckYMin = 1f;

    // 天井コライダーを誤検知しないよう、上限は天井より低めに設定する。
    [Tooltip("コライダー検知ボックスの Y 最大値")]
    public float NavCheckYMax = 3f;

    // 1.0 にするとセル境界上の薄い壁も拾いやすくなるが、隣接セルへの誤検知が増える。
    [Tooltip("ナビセル XZ 方向の検知サイズ係数 (0.5〜1.0)")]
    [Range(0.5f, 1.0f)]
    public float NavCheckXZScale = 0.85f;
}
