using UnityEngine;

/// <summary>
/// マップ全体の設計図
/// </summary>
[CreateAssetMenu(fileName = "FieldBluePrint", menuName = "Dungeon/FieldBluePrint")]
public class FieldBluePrint : ScriptableObject
{
    public float gridSize = 5f;

    public Vector2Int mapSize;           // マップ全体のグリッドサイズ
    public SectionData[] sections;

    // 全Section接続後に追加するランダム通路の本数範囲
    public int minExtraBranchNum;
    public int maxExtraBranchNum;
}