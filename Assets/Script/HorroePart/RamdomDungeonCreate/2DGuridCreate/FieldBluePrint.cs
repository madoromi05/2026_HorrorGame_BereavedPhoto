using UnityEngine;

/// <summary>
/// マップ全体の設計図
/// </summary>
[CreateAssetMenu(fileName = "FieldBluePrint", menuName = "Dungeon/FieldBluePrint")]
public class FieldBluePrint : ScriptableObject
{
    public float OneGridSize = 5f;      // 1グリッドの実際のサイズ（ユニティのワールド単位）

    public Vector2Int MapSize;           // マップ全体のグリッドサイズ
    public Vector2Int SectionDivide;     // セクション分割数

    // 全Section接続後に追加するランダム通路の本数範囲
    public int MinExtraBranchNum;
    public int MaxExtraBranchNum;

    [Range(1, 3)] public int CorridorWidth;
}