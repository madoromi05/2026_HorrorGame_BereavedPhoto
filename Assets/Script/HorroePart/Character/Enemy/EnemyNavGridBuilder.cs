using DungeonSystem;
using UnityEngine;

/// <summary>
/// ダンジョングリッドとセクション情報から、敵移動用の細粒度ナビグリッドを生成する静的ユーティリティ。
///
/// 生成ステップ:
///   1. ダンジョングリッドを navSubdivision 倍の解像度に拡大し、基本通行可否を設定する
///      - Corridor / Door / Floor / PlayerPosition → 通行可
///      - Empty / Wall → 通行不可
///      - 通路セルは常に通行可（部屋の Wall 設定より優先されない）
///   2. 各部屋の RoomGridData.EnemyNavWallCells で細粒度の移動不可セルを上書きする
///
/// navCellSize = dungeonCellSize / navSubdivision
/// </summary>
public static class EnemyNavGridBuilder
{
    /// <summary>
    /// ナビグリッドを構築して返す。
    /// </summary>
    /// <param name="dungeonGrid">DungeonGridBuilder が生成したグリッド</param>
    /// <param name="dungeonCellSize">1ダンジョンセルのワールド単位サイズ（FieldBluePrint.OneGridSize）</param>
    /// <param name="navSubdivision">1ダンジョンセルをナビセル何分割するか（FieldBluePrint.EnemyNavSubdivision）</param>
    /// <param name="sections">全セクション情報（部屋の RoomGridData を参照する）</param>
    /// <returns>通行可能なら true のナビグリッド</returns>
    public static bool[,] Build(
        GridType[,] dungeonGrid,
        float dungeonCellSize,
        int navSubdivision,
        SectionData[] sections)
    {
        navSubdivision = Mathf.Max(1, navSubdivision);

        int dungeonW = dungeonGrid.GetLength(0);
        int dungeonH = dungeonGrid.GetLength(1);
        int navW = dungeonW * navSubdivision;
        int navH = dungeonH * navSubdivision;

        var walkable = new bool[navW, navH];

        // ステップ1: ダンジョングリッドから基本通行可否を設定
        for (int dx = 0; dx < dungeonW; dx++)
        {
            for (int dy = 0; dy < dungeonH; dy++)
            {
                bool isWalkable = IsBaseWalkable(dungeonGrid[dx, dy]);
                int baseNX = dx * navSubdivision;
                int baseNY = dy * navSubdivision;

                for (int sx = 0; sx < navSubdivision; sx++)
                    for (int sy = 0; sy < navSubdivision; sy++)
                        walkable[baseNX + sx, baseNY + sy] = isWalkable;
            }
        }

        // ステップ2: 部屋ごとの EnemyNavWallCells で細粒度障害物を追加
        if (sections != null)
        {
            foreach (var section in sections)
            {
                var roomData = section.RoomGridData;
                if (roomData == null
                    || roomData.EnemyNavWallCells == null
                    || roomData.EnemyNavWallCells.Count == 0) continue;

                // 部屋のナビグリッド原点（グローバルナビ座標）
                int originNX = section.RoomGridPosition.x * navSubdivision;
                int originNY = section.RoomGridPosition.y * navSubdivision;

                foreach (var localCell in roomData.EnemyNavWallCells)
                {
                    int gnx = originNX + localCell.x;
                    int gny = originNY + localCell.y;
                    if (gnx >= 0 && gnx < navW && gny >= 0 && gny < navH)
                        walkable[gnx, gny] = false;
                }
            }
        }

        return walkable;
    }

    /// <summary>navCellSize = dungeonCellSize / navSubdivision を返す。</summary>
    public static float GetNavCellSize(float dungeonCellSize, int navSubdivision)
        => dungeonCellSize / Mathf.Max(1, navSubdivision);

    private static bool IsBaseWalkable(GridType cell)
        => cell == GridType.Floor
        || cell == GridType.Door
        || cell == GridType.Corridor
        || cell == GridType.PlayerPosition;
}
