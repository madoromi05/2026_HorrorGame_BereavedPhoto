using UnityEngine;

/// <summary>
/// マップ全体の設計図
/// </summary>
[CreateAssetMenu(fileName = "FieldBluePrint", menuName = "Dungeon/FieldBluePrint")]
public class FieldBluePrint : ScriptableObject
{
    public float OneGridSize = 8f;      // 1グリッドの実際のサイズ（ユニティのワールド単位）

    public Vector2Int MapSize;           // マップ全体のグリッドサイズ
    public Vector2Int SectionDivide;     // セクション分割数

    // 追加通路を生成する最小距離（どちらのセクションから見ても接続点がこの値以上離れていること）
    public int MinExtraCorridorLength = 8;
}