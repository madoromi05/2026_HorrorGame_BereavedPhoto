using DungeonSystem;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.CoreUtils;

/// <summary>
/// FieldBluePrintの情報をもとに、2Dのダンジョングリッドを構築するクラス
/// </summary>
public class DungeonGridBuilder
{
    private GridType[,] _grid;
    private FieldBluePrint _bluePrint;
    private SectionData[] _sections;

    // セクション毎 → グリッド上のDoor座標リスト
    private Dictionary<SectionData, List<Vector2Int>> _sectionDoorMap;

    // 部屋なし → 通路の中心点座標
    private Dictionary<SectionData, Vector2Int> _pathPointMap;

    // 設計図を受け取り、2Dグリッドを構築して返す
    public (GridType[,] grid, SectionData[] sections) Build(FieldBluePrint bluePrint, RoomDataBase roomDataBase)
    {
        _bluePrint = bluePrint;
        _grid = new GridType[bluePrint.MapSize.x, bluePrint.MapSize.y];
        _sectionDoorMap = new Dictionary<SectionData, List<Vector2Int>>();
        _pathPointMap = new Dictionary<SectionData, Vector2Int>();

        _sections = GenerateSections(roomDataBase);

        PlaceRooms();
        LogGridStats("After PlaceRooms");
        ConnectSections();
        LogGridStats("After ConnectSections");
        AddExtraBranches();
        LogGridStats("After AddExtraBranches");
        ConnectUnconnectedDoors();
        LogGridStats("After ConnectUnconnectedDoors");

        return (_grid, _sections);
    }

    // Startは必ず1、残りはNormalとしてSectionをランダムに割り当てる
    private SectionData[] GenerateSections(RoomDataBase roomDataBase)
    {
        var divide = _bluePrint.SectionDivide;

        // マージンを考慮してセクションのサイズを計算
        const int kMargin = 1;
        var innerSize = new Vector2Int(
       _bluePrint.MapSize.x - kMargin * 2,
       _bluePrint.MapSize.y - kMargin * 2
        );
        var sectionSize = new Vector2Int(
            innerSize.x / divide.x,
            innerSize.y / divide.y
        );

        int totalCount = divide.x * divide.y;
        var sections = new SectionData[totalCount];

        int startIndex = Random.Range(0, totalCount);

        for (int x = 0; x < divide.x; x++)
        {
            for (int y = 0; y < divide.y; y++)
            {
                int index = x + y * divide.x;
                var role = index == startIndex ? RoomType.Start : RoomType.Normal;

                sections[index] = new SectionData
                {
                    GridPosition = new Vector2Int(
                        kMargin + x * sectionSize.x,
                        kMargin + y * sectionSize.y
                    ),
                    GridSize = sectionSize,
                    Role = role,
                    RoomGridData = (role == RoomType.Start || Random.value < 0.5f) ? roomDataBase.GetRandomRoomGridData(role) : null
                };
            }
        }

        return sections;
    }

    private void PlaceRooms()
    {
        foreach (var section in _sections)
        {
            if (section.RoomGridData != null)
                PlaceRoomGrid(section);
            else
                PlacePath(section);
        }
    }

    // RoomGridDataをグリッドに書き込む
    // 部屋はsection.GridPositionを起点に配置する（FBXの固定壁と座標を合わせるため）
    private void PlaceRoomGrid(SectionData section)
    {
        var roomData = section.RoomGridData;
        var doorPositions = new List<Vector2Int>();

        // セクション内でランダムオフセットを計算（1セル以上のマージンを確保）
        int spaceX = section.GridSize.x - roomData.GridSize.x;
        int spaceY = section.GridSize.y - roomData.GridSize.y;
        int offsetX = spaceX >= 2 ? Random.Range(1, spaceX) : 0;
        int offsetY = spaceY >= 2 ? Random.Range(1, spaceY) : 0;
        section.RoomGridPosition = section.GridPosition + new Vector2Int(offsetX, offsetY);

        Debug.Log($"[Grid] RoomGridPosition={section.RoomGridPosition} GridSize={roomData.GridSize}");

        for (int x = 0; x < roomData.GridSize.x; x++)
        {
            for (int y = 0; y < roomData.GridSize.y; y++)
            {
                var localPos = new Vector2Int(x, y);
                var worldPos = section.RoomGridPosition + localPos;
                if (!IsInGrid(worldPos)) continue;

                if (roomData.DoorPositions.Contains(localPos))
                {
                    _grid[worldPos.x, worldPos.y] = GridType.Door;
                    doorPositions.Add(worldPos);
                }
                else if (roomData.WallPositions != null && roomData.WallPositions.Contains(localPos))
                {
                    _grid[worldPos.x, worldPos.y] = GridType.Wall;
                }
                else
                {
                    _grid[worldPos.x, worldPos.y] = GridType.Floor;
                }
            }
        }
        _sectionDoorMap[section] = doorPositions;
    }

    private void PlacePath(SectionData section)
    {
        var center = section.GridPosition + section.GridSize / 2;
        _pathPointMap[section] = center;
    }

    private void ConnectSections()
    {
        var connectedSections = new HashSet<SectionData>();
        connectedSections.Add(_sections[0]);

        while (connectedSections.Count < _sections.Length)
        {
            SectionData bestFrom = null;
            SectionData bestTo = null;
            float minDist = float.MaxValue;

            foreach (var connected in connectedSections)
            {
                foreach (var section in _sections)
                {
                    if (connectedSections.Contains(section)) continue;

                    var connectedCenter = connected.GridPosition + connected.GridSize / 2;
                    var sectionCenter = section.GridPosition + section.GridSize / 2;
                    var dist = Vector2Int.Distance(connectedCenter, sectionCenter);

                    if (dist < minDist)
                    {
                        minDist = dist;
                        bestFrom = connected;
                        bestTo = section;
                    }
                }
            }

            if (bestFrom == null || bestTo == null) break;

            ConnectTwoSections(bestFrom, bestTo);
            connectedSections.Add(bestTo);
        }
    }

    private void AddExtraBranches()
    {
        int extraCount = Random.Range(_bluePrint.MinExtraBranchNum, _bluePrint.MaxExtraBranchNum + 1);

        for (int i = 0; i < extraCount; i++)
        {
            var from = _sections[Random.Range(0, _sections.Length)];
            var to = _sections[Random.Range(0, _sections.Length)];
            if (from == to) continue;
            ConnectTwoSections(from, to);
        }
    }

    private void ConnectTwoSections(SectionData from, SectionData to)
    {
        var startPos = GetConnectionPoint(from, to);
        var endPos = GetConnectionPoint(to, from);

        var path = RunAStar(startPos, endPos);
        if (path == null) { DebugCustom.LogWarning($"[DungeonGrid] A* failed: {startPos} -> {endPos}"); return; }

        foreach (var pos in path)
        {
            if (_grid[pos.x, pos.y] == GridType.Door) continue;
            if (_grid[pos.x, pos.y] == GridType.Floor) continue;
            if (_grid[pos.x, pos.y] == GridType.Corridor) continue;
            _grid[pos.x, pos.y] = GridType.Corridor;
        }
    }

    private Vector2Int GetConnectionPoint(SectionData section, SectionData target)
    {
        var targetCenter = target.GridPosition + target.GridSize / 2;

        if (_sectionDoorMap.TryGetValue(section, out var doors))
            return FindNearest(doors, targetCenter);

        return _pathPointMap[section];
    }

    private Vector2Int FindNearest(List<Vector2Int> positions, Vector2Int targetPos)
    {
        if (positions == null || positions.Count == 0)
        {
            DebugCustom.LogWarning("FindNearest: DoorPositionsが空です。RoomGridDataのDoorPositionsを確認してください。");
            return targetPos;
        }

        var nearest = positions[0];
        var minDist = float.MaxValue;

        foreach (var pos in positions)
        {
            var dist = Vector2Int.Distance(pos, targetPos);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = pos;
            }
        }

        return nearest;
    }

    /// <summary>
    /// A*でstartからendまでの経路を返す
    /// WallはA*のブロック対象。
    /// 部屋内部（Door以外のFloor）もブロックし、通路がDoorセル経由でのみ部屋に繋がるようにする。
    /// 経路が見つからない場合はnullを返す
    /// </summary>
    private List<Vector2Int> RunAStar(Vector2Int start, Vector2Int end)
    {
        var openSet = new SortedSet<(float f, Vector2Int pos)>(
            Comparer<(float f, Vector2Int pos)>.Create((a, b) =>
                a.f != b.f ? a.f.CompareTo(b.f) :
                a.pos.x != b.pos.x ? a.pos.x.CompareTo(b.pos.x) :
                a.pos.y.CompareTo(b.pos.y)
            )
        );

        var gCost = new Dictionary<Vector2Int, float>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        gCost[start] = 0f;
        openSet.Add((Heuristic(start, end), start));

        var neighbors = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (openSet.Count > 0)
        {
            var (_, current) = openSet.Min;
            openSet.Remove(openSet.Min);

            if (current == end)
                return BuildPath(cameFrom, current);

            foreach (var dir in neighbors)
            {
                var neighbor = current + dir;
                if (!IsInGrid(neighbor)) continue;
                if (_grid[neighbor.x, neighbor.y] == GridType.Wall) continue;
                // 部屋内部（Door以外）はA*通過不可：Doorセル経由でのみ接続させる
                if (_grid[neighbor.x, neighbor.y] == GridType.Floor) continue;

                float moveCost = _grid[neighbor.x, neighbor.y] switch
                {
                    GridType.Door     => 0.5f,
                    GridType.Corridor => 0.5f,
                    _ => 1.0f
                };

                float newG = gCost[current] + moveCost;
                if (gCost.TryGetValue(neighbor, out float existingG) && newG >= existingG) continue;

                gCost[neighbor] = newG;
                cameFrom[neighbor] = current;
                openSet.Add((newG + Heuristic(neighbor, end), neighbor));
            }
        }

        return null;
    }

    private float Heuristic(Vector2Int a, Vector2Int b)
        => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    private List<Vector2Int> BuildPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        var path = new List<Vector2Int>();
        while (cameFrom.ContainsKey(current))
        {
            path.Add(current);
            current = cameFrom[current];
        }
        path.Add(current);
        path.Reverse();
        return path;
    }

    private bool IsInGrid(Vector2Int pos)
        => pos.x >= 0 && pos.x < _grid.GetLength(0)
        && pos.y >= 0 && pos.y < _grid.GetLength(1);

    private void LogGridStats(string label)
    {
        int empty = 0, floor = 0, wall = 0, door = 0, corridor = 0;
        for (int x = 0; x < _grid.GetLength(0); x++)
            for (int y = 0; y < _grid.GetLength(1); y++)
                switch (_grid[x, y])
                {
                    case GridType.Empty: empty++; break;
                    case GridType.Floor: floor++; break;
                    case GridType.Wall: wall++; break;
                    case GridType.Door: door++; break;
                    case GridType.Corridor: corridor++; break;
                }
        DebugCustom.Log($"[DungeonGrid] {label} -> Empty:{empty} Floor:{floor} Wall:{wall} Door:{door} Corridor:{corridor}");
    }

    /// <summary>
    /// セクション間接続後も通路と繋がっていないDoorを検出し、
    /// グリッド上の最近傍Floor/Doorセルへ向けてA*で通路を延伸する。
    /// 全Doorが必ず通路ネットワークに参加することを保証するための後処理。
    /// </summary>
    private void ConnectUnconnectedDoors()
    {
        var corridorCells = CollectCorridorCells();
        // ConnectSections が全て失敗した場合など corridorCells が空のケースでは
        // 全セクションを強制的に1本繋いでから再収集し、接続先が必ず存在する状態にする
        if (corridorCells.Count == 0)
        {
            DebugCustom.LogWarning("[DungeonGrid] corridorCells が空のため強制接続を実行します");
            for (int i = 0; i < _sections.Length - 1; i++)
                ConnectTwoSections(_sections[i], _sections[i + 1]);
            corridorCells = CollectCorridorCells();
        }

        // 強制接続後もCorridorが生成できなかった場合は処理不能なので中断する
        if (corridorCells.Count == 0)
        {
            DebugCustom.LogWarning("[DungeonGrid] 強制接続後もcorridorCellsが空です。マップ設定を確認してください");
            return;
        }
        foreach (var (section, doors) in _sectionDoorMap)
        {
            foreach (var doorPos in doors)
            {
                if (IsDoorConnected(doorPos)) continue;

                // DoorはFloor（部屋内部）に囲まれているためA*の起点にできない。
                // Doorの隣のEmptyセル（部屋の外側方向）を起点にすることで
                // A*が部屋内部をすり抜けずに通路を延伸できる。
                var astarStart = FindExitCell(doorPos);
                if (astarStart == null)
                {
                    DebugCustom.LogWarning($"[DungeonGrid] Door {doorPos} の外側出口セルが見つかりません");
                    continue;
                }

                // corridorCells が空の場合（ConnectSections が全失敗した極端なケース）は
                // 他セクションの中心点を代替接続先として使い、孤立したままにしない
                var target = FindNearestCorridorCell(astarStart.Value, corridorCells);
                if (target == null)
                {
                    DebugCustom.LogWarning($"[DungeonGrid] 孤立Door {doorPos} の接続先が見つかりませんでした");
                    continue;
                }

                var path = RunAStar(astarStart.Value, target.Value);
                if (path == null)
                {
                    DebugCustom.LogWarning($"[DungeonGrid] 孤立Door A* 失敗: {astarStart.Value} -> {target.Value}");
                    continue;
                }

                // Doorと出口セルを含めて通路として書き込む
                _grid[astarStart.Value.x, astarStart.Value.y] = GridType.Corridor;
                foreach (var pos in path)
                {
                    if (_grid[pos.x, pos.y] == GridType.Door) continue;
                    if (_grid[pos.x, pos.y] == GridType.Floor) continue;
                    if (_grid[pos.x, pos.y] == GridType.Corridor) continue;
                    _grid[pos.x, pos.y] = GridType.Corridor;
                }

                corridorCells.Add(astarStart.Value);
                corridorCells.UnionWith(path);
            }
        }
    }

    /// <summary>
    /// Doorの隣にあるEmptyセル（部屋の外側方向の出口）を返す。
    /// 全方向EmptyでなければnullをReturn。
    /// </summary>
    private Vector2Int? FindExitCell(Vector2Int doorPos)
    {
        foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
        {
            var neighbor = doorPos + dir;
            if (!IsInGrid(neighbor)) continue;
            if (_grid[neighbor.x, neighbor.y] == GridType.Empty)
                return neighbor;
        }
        return null;
    }

    /// <summary>
    /// 部屋内部を除くFloor/Doorセルを「接続済み通路セル」として収集する。
    /// _roomInteriorCells に含まれないFloorのみを対象とする。
    /// </summary>
    private HashSet<Vector2Int> CollectCorridorCells()
    {
        var cells = new HashSet<Vector2Int>();
        for (int x = 0; x < _grid.GetLength(0); x++)
        {
            for (int y = 0; y < _grid.GetLength(1); y++)
            {
                var type = _grid[x, y];
                if (type == GridType.Corridor)
                    cells.Add(new Vector2Int(x, y));
            }
        }
        return cells;
    }

    /// <summary>
    /// DoorセルのN/E/S/W方向に通路Floor（部屋内部を除く）が隣接していれば接続済みと判定する。
    /// </summary>
    private bool IsDoorConnected(Vector2Int doorPos)
    {
        foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
        {
            var neighbor = doorPos + dir;
            if (!IsInGrid(neighbor)) continue;
            if (_grid[neighbor.x, neighbor.y] == GridType.Corridor) return true;
        }
        return false;
    }

    /// <summary>
    /// 候補セルの中からdoorPosに最も近いセルを返す。
    /// 候補が空の場合はnullを返す。
    /// </summary>
    private Vector2Int? FindNearestCorridorCell(Vector2Int doorPos, HashSet<Vector2Int> candidates)
    {
        Vector2Int? nearest = null;
        float minDist = float.MaxValue;

        foreach (var pos in candidates)
        {
            float dist = Vector2Int.Distance(doorPos, pos);
            if (dist >= minDist) continue;
            minDist = dist;
            nearest = pos;
        }

        return nearest;
    }
}
