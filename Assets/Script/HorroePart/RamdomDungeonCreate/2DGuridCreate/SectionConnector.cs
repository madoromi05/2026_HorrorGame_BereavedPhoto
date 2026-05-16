/// <summary>
/// セクション間を A* で接続する通路生成クラス。
/// MST（最小全域木）による全セクション接続と、追加分岐の生成を担当する。
/// グリッドの書き込みは行うが、セクションの生成・部屋配置には関与しない。
/// </summary>
using DungeonSystem;
using System.Collections.Generic;
using UnityEngine;

public class SectionConnector
{
    private GridType[,] _grid;
    private SectionData[] _sections;
    private Dictionary<SectionData, List<Vector2Int>> _sectionDoorMap;
    private Dictionary<SectionData, Vector2Int> _pathPointMap;

    /// <summary>
    /// MST で全セクションを接続したあと、追加分岐を生成する。
    /// </summary>
    public void Connect(
        GridType[,] grid,
        SectionData[] sections,
        Dictionary<SectionData, List<Vector2Int>> sectionDoorMap,
        Dictionary<SectionData, Vector2Int> pathPointMap,
        FieldBluePrint bluePrint)
    {
        _grid = grid;
        _sections = sections;
        _sectionDoorMap = sectionDoorMap;
        _pathPointMap = pathPointMap;

        ConnectAllSections();
        AddExtraBranches(bluePrint);
    }

    // Prim 法に近い最小全域木でセクションを順番に接続する
    private void ConnectAllSections()
    {
        var connected = new HashSet<SectionData> { _sections[0] };

        while (connected.Count < _sections.Length)
        {
            SectionData bestFrom = null;
            SectionData bestTo = null;
            float minDist = float.MaxValue;

            foreach (var from in connected)
            {
                foreach (var to in _sections)
                {
                    if (connected.Contains(to)) continue;

                    float dist = Vector2Int.Distance(
                        from.GridPosition + from.GridSize / 2,
                        to.GridPosition + to.GridSize / 2
                    );

                    if (dist < minDist)
                    {
                        minDist = dist;
                        bestFrom = from;
                        bestTo = to;
                    }
                }
            }

            if (bestFrom == null || bestTo == null) break;

            ConnectTwoSections(bestFrom, bestTo);
            connected.Add(bestTo);
        }
    }

    private void AddExtraBranches(FieldBluePrint bluePrint)
    {
        int extraCount = Random.Range(bluePrint.MinExtraBranchNum, bluePrint.MaxExtraBranchNum + 1);
        const int kMaxRetry = 10;

        for (int i = 0; i < extraCount; i++)
        {
            SectionData from, to;
            int retry = 0;
            do
            {
                from = _sections[Random.Range(0, _sections.Length)];
                to = _sections[Random.Range(0, _sections.Length)];
            } while (from == to && ++retry < kMaxRetry);

            if (from == to) continue;
            ConnectTwoSections(from, to);
        }
    }

    /// <summary>
    /// 2 セクション間を A* で接続し、経路上のセルを Corridor として書き込む。
    /// 既存の Door / Floor / Corridor セルは上書きしない。
    /// </summary>
    public void ConnectTwoSections(SectionData from, SectionData to)
    {
        var startPos = GetConnectionPoint(from, to);
        var endPos = GetConnectionPoint(to, from);

        var path = RunAStar(startPos, endPos);
        if (path == null)
        {
            DebugCustom.LogWarning($"[SectionConnector] A* 失敗: {startPos} -> {endPos}");
            return;
        }

        foreach (var pos in path)
        {
            var cellType = _grid[pos.x, pos.y];
            if (cellType == GridType.Door) continue;
            if (cellType == GridType.Floor) continue;
            if (cellType == GridType.Corridor) continue;
            _grid[pos.x, pos.y] = GridType.Corridor;
        }
    }

    // 対象セクションの接続点を返す。
    // 部屋ありなら相手の中心に最も近い Door、部屋なしなら PathPoint を使う。
    private Vector2Int GetConnectionPoint(SectionData section, SectionData target)
    {
        var targetCenter = target.GridPosition + target.GridSize / 2;

        if (_sectionDoorMap.TryGetValue(section, out var doors))
            return FindNearest(doors, targetCenter);

        return _pathPointMap[section];
    }

    private Vector2Int FindNearest(List<Vector2Int> positions, Vector2Int target)
    {
        if (positions == null || positions.Count == 0)
        {
            DebugCustom.LogWarning("[SectionConnector] FindNearest: DoorPositions が空です。RoomGridData を確認してください。");
            return target;
        }

        var nearest = positions[0];
        float minDist = float.MaxValue;

        foreach (var pos in positions)
        {
            float dist = Vector2Int.Distance(pos, target);
            if (dist >= minDist) continue;
            minDist = dist;
            nearest = pos;
        }

        return nearest;
    }

    /// <summary>
    /// A* で start から end までの経路を返す。
    /// Wall と部屋内部（Door 以外の Floor）は通過不可。
    /// Door / Corridor は移動コストを低くし、既存通路への合流を優先する。
    /// 経路が見つからない場合は null を返す。
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

                var cellType = _grid[neighbor.x, neighbor.y];
                if (cellType == GridType.Wall) continue;
                // 部屋内部は Door 経由でのみ接続させる
                if (cellType == GridType.Floor) continue;

                float moveCost = cellType switch
                {
                    GridType.Door => 0.5f,
                    GridType.Corridor => 0.5f,
                    _ => 1.0f,
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
}