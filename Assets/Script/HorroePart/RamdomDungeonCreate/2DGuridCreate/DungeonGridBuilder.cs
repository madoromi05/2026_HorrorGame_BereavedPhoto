/// <summary>
/// FieldBluePrint をもとに 2D ダンジョングリッドを構築するオーケストレーター。
/// 各フェーズの実行順序を制御するのみで、ロジックは各専門クラスに委譲する。
///
/// フェーズ順:
///   1. SectionGenerator    - セクション生成（分割・役割割り当て）
///   2. RoomGridPlacer      - グリッドへの部屋・パスポイント書き込み
///   3. SectionConnector    - MST + 追加分岐による通路生成（A*）
///   4. IsolatedDoorRepairer- 孤立 Door の後処理補修
/// </summary>
using DungeonSystem;
using UnityEngine;

public class DungeonGridBuilder
{
    private SectionGenerator _sectionGenerator;
    private RoomGridPlacer _roomGridPlacer;
    private SectionConnector _sectionConnector;
    private IsolatedDoorRepairer _isolatedDoorRepairer;

    public DungeonGridBuilder()
    {
        _sectionGenerator = new SectionGenerator();
        _roomGridPlacer = new RoomGridPlacer();
        _sectionConnector = new SectionConnector();
        _isolatedDoorRepairer = new IsolatedDoorRepairer();
    }

    /// <summary>
    /// ダンジョングリッドを構築して返す。
    /// </summary>
    public (GridType[,] grid, SectionData[] sections) Build(FieldBluePrint bluePrint, RoomDataBase roomDataBase)
    {
        var grid = new GridType[bluePrint.MapSize.x, bluePrint.MapSize.y];
        var sections = _sectionGenerator.Generate(bluePrint, roomDataBase);

        _roomGridPlacer.Place(grid, sections,
            out var sectionDoorMap,
            out var pathPointMap);
        LogGridStats(grid, "After PlaceRooms");

        _sectionConnector.Connect(grid, sections, sectionDoorMap, pathPointMap, bluePrint);
        LogGridStats(grid, "After ConnectSections");

        _isolatedDoorRepairer.Repair(grid, sectionDoorMap, _sectionConnector, sections);
        LogGridStats(grid, "After ConnectUnconnectedDoors");

        return (grid, sections);
    }

    private void LogGridStats(GridType[,] grid, string label)
    {
        int empty = 0, floor = 0, wall = 0, door = 0, corridor = 0;
        for (int x = 0; x < grid.GetLength(0); x++)
            for (int y = 0; y < grid.GetLength(1); y++)
                switch (grid[x, y])
                {
                    case GridType.Empty: empty++; break;
                    case GridType.Floor: floor++; break;
                    case GridType.Wall: wall++; break;
                    case GridType.Door: door++; break;
                    case GridType.Corridor: corridor++; break;
                }
        DebugCustom.Log($"[DungeonGridBuilder] {label} -> Empty:{empty} Floor:{floor} Wall:{wall} Door:{door} Corridor:{corridor}");
    }
}