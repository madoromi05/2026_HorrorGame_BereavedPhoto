/// <summary>
/// FieldBluePrint をもとにセクションを生成するクラス。
/// マップ分割・役割割り当て・RoomGridData のアサインのみを担当し、
/// グリッドへの書き込みは行わない。
/// </summary>
using DungeonSystem;
using System.Collections.Generic;
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

        // セクションのグリッド座標を先に確定し、シャッフルして割り当て順を決める
        var shuffledIndices = CreateShuffledIndices(totalCount);

        // RoomDataBase の定義順に MaxCount 分のロールを割り当てる。
        // セクション数が合計 MaxCount に満たない場合は先着優先で打ち切る。
        var roleAssignments = BuildRoleAssignments(roomDataBase, shuffledIndices);

        for (int x = 0; x < divide.x; x++)
        {
            for (int y = 0; y < divide.y; y++)
            {
                int index = x + y * divide.x;
                roleAssignments.TryGetValue(index, out var assignment);

                sections[index] = new SectionData
                {
                    GridPosition = new Vector2Int(
                        kMargin + x * sectionSize.x,
                        kMargin + y * sectionSize.y
                    ),
                    GridSize = sectionSize,
                    Role = assignment.role,
                    // roleAssignments に含まれないセクションは通路点扱い（RoomGridData = null）
                    RoomGridData = roleAssignments.ContainsKey(index)
                        ? roomDataBase.GetRoomGridData(assignment.role, assignment.idx)
                        : null,
                };
            }
        }

        return sections;
    }

    /// <summary>
    /// 0 〜 totalCount-1 のインデックスをランダムな順番に並べたリストを返す。
    /// セクションへのロール割り当て順序をランダム化するために使用する。
    /// </summary>
    private List<int> CreateShuffledIndices(int totalCount)
    {
        var indices = new List<int>(totalCount);
        for (int i = 0; i < totalCount; i++)
            indices.Add(i);

        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
        return indices;
    }

    /// <summary>
    /// RoomDataBase に登録された全 RoomGridData を1件ずつセクションへ割り当てる。
    /// セクション総数を超えた分は無視し、余ったセクションは通路点になる。
    /// </summary>
    private Dictionary<int, (RoomType role, int idx)> BuildRoleAssignments(RoomDataBase roomDataBase, List<int> shuffledIndices)
    {
        var assignments = new Dictionary<int, (RoomType role, int idx)>();
        int cursor = 0;

        foreach (var roomType in System.Enum.GetValues(typeof(RoomType)) as RoomType[])
        {
            int count = roomDataBase.GetRoomGridDataCount(roomType);
            for (int i = 0; i < count; i++)
            {
                if (cursor >= shuffledIndices.Count)
                {
                    DebugCustom.LogWarning($"[SectionGenerator] セクション数が不足しています。{roomType} の割り当てを打ち切ります。SectionDivideを増やしてください。");
                    return assignments;
                }
                assignments[shuffledIndices[cursor]] = (roomType, i);
                cursor++;
            }
        }

        return assignments;
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