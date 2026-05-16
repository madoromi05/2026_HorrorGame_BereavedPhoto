/// <summary>
/// FieldBluePrint をもとにセクションを生成するクラス。
/// マップ分割・役割割り当て・RoomGridData のアサインのみを担当し、
/// グリッドへの書き込みは行わない。
/// </summary>
using DungeonSystem;
using UnityEngine;

public class SectionGenerator
{
    // マップ外周との境界マージン（グリッド単位）
    private const int kMargin = 1;

    /// <summary>
    /// bluePrint の分割設定に従い SectionData 配列を生成して返す。
    /// Start セクションはランダムに 1 つ選ばれ、残りは Normal になる。
    /// </summary>
    public SectionData[] Generate(FieldBluePrint bluePrint, RoomDataBase roomDataBase)
    {
        var divide = bluePrint.SectionDivide;
        var sectionSize = CalcSectionSize(bluePrint);

        int totalCount = divide.x * divide.y;
        var sections = new SectionData[totalCount];
        int startIndex = Random.Range(0, totalCount);

        for (int x = 0; x < divide.x; x++)
        {
            for (int y = 0; y < divide.y; y++)
            {
                int index = x + y * divide.x;
                var role = (index == startIndex) ? RoomType.Start : RoomType.Normal;

                sections[index] = new SectionData
                {
                    GridPosition = new Vector2Int(
                        kMargin + x * sectionSize.x,
                        kMargin + y * sectionSize.y
                    ),
                    GridSize = sectionSize,
                    Role = role,
                    // Start は必ず部屋あり。Normal は 50% でランダム配置
                    RoomGridData = (role == RoomType.Start || Random.value < 0.5f)
                        ? roomDataBase.GetRandomRoomGridData(role)
                        : null,
                };
            }
        }

        return sections;
    }

    private Vector2Int CalcSectionSize(FieldBluePrint bluePrint)
    {
        var divide = bluePrint.SectionDivide;
        var innerSize = new Vector2Int(
            bluePrint.MapSize.x - kMargin * 2,
            bluePrint.MapSize.y - kMargin * 2
        );
        return new Vector2Int(
            innerSize.x / divide.x,
            innerSize.y / divide.y
        );
    }
}