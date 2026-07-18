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

    // 一直線に見通せる最大ワールド距離。描画距離上限の暗転が見えないよう、
    // この距離を OneGridSize で割った値を通路A*の直進マス上限に使う（0以下で無制限）。
    public float MaxStraightSightWorld = 50f;

    // 直進マス上限（MaxStraightSightWorld をグリッド換算した派生値。インスペクタでは上の距離を調整する）
    public int MaxStraightCells =>
        MaxStraightSightWorld <= 0f ? 0 : Mathf.FloorToInt(MaxStraightSightWorld / OneGridSize);
}