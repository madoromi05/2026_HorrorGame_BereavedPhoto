using DungeonSystem;
using UnityEngine;

/// <summary>
/// ダンジョングリッドとセクション情報から、敵移動用の細粒度ナビグリッドを生成する静的ユーティリティ。
/// </summary>
public static class EnemyNavGridBuilder
{
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
        var baseWalkable = new bool[navW, navH];

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
                if (roomData == null || roomData.EnemyNavWallCells == null) continue;

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

    public static float GetNavCellSize(float dungeonCellSize, int navSubdivision)
        => dungeonCellSize / Mathf.Max(1, navSubdivision);

    private static bool IsBaseWalkable(GridType cell)
        => cell == GridType.Floor
        || cell == GridType.Door
        || cell == GridType.Corridor
        || cell == GridType.PlayerPosition;
}
