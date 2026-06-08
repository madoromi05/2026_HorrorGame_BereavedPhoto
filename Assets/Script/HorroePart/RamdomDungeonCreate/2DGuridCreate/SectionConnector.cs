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
    private int _corridorWidth;
    private int _maxCorridorLength;

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
        _corridorWidth = bluePrint.CorridorWidth;
        _maxCorridorLength = bluePrint.MaxCorridorLength;

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

        // MaxCorridorLength が有効なとき、近距離ペアだけを候補に絞る
        var candidates = BuildNearPairs();

        const int kMaxRetry = 20;
        for (int i = 0; i < extraCount; i++)
        {
            if (candidates.Count > 0)
            {
                var (from, to) = candidates[Random.Range(0, candidates.Count)];
                ConnectTwoSections(from, to);
            }
            else
            {
                // フォールバック：距離制限なしでランダム選択
                SectionData from, to;
                int retry = 0;
                do
                {
                    from = _sections[Random.Range(0, _sections.Length)];
                    to   = _sections[Random.Range(0, _sections.Length)];
                } while (from == to && ++retry < kMaxRetry);

                if (from != to)
                    ConnectTwoSections(from, to);
            }
        }
    }

    // セクション中心間のマンハッタン距離が MaxCorridorLength 以内のペアを列挙する。
    // MaxCorridorLength <= 0 の場合は全ペアを返す。
    private List<(SectionData, SectionData)> BuildNearPairs()
    {
        var result = new List<(SectionData, SectionData)>();
        for (int i = 0; i < _sections.Length; i++)
        {
            for (int j = i + 1; j < _sections.Length; j++)
            {
                if (_maxCorridorLength > 0)
                {
                    var ci = _sections[i].GridPosition + _sections[i].GridSize / 2;
                    var cj = _sections[j].GridPosition + _sections[j].GridSize / 2;
                    int dist = Mathf.Abs(ci.x - cj.x) + Mathf.Abs(ci.y - cj.y);
                    if (dist > _maxCorridorLength) continue;
                }
                result.Add((_sections[i], _sections[j]));
            }
        }
        return result;
    }

    /// <summary>
    /// 2 セクション間を A* で接続し、経路上のセルを Corridor として書き込む。
    /// 既存の Door / Floor / Corridor セルは上書きしない。
    /// MaxCorridorLength を超える経路は書き込まずに false を返す。
    /// </summary>
    public bool ConnectTwoSections(SectionData from, SectionData to)
    {
        var startPos = GetConnectionPoint(from, to);
        var endPos = GetConnectionPoint(to, from);

        var path = RunAStar(startPos, endPos);
        if (path == null)
        {
            DebugCustom.LogWarning($"[SectionConnector] A* 失敗: {startPos} -> {endPos}");
            return false;
        }

        if (_maxCorridorLength > 0 && path.Count > _maxCorridorLength)
        {
            DebugCustom.Log($"[SectionConnector] 通路長 {path.Count} が上限 {_maxCorridorLength} を超えたためスキップ");
            return false;
        }

        var newlyPainted = new List<Vector2Int>();
        foreach (var pos in path)
        {
            var cellType = _grid[pos.x, pos.y];
            if (cellType == GridType.Door) continue;
            if (cellType == GridType.Floor) continue;
            if (cellType == GridType.Corridor) continue;
            _grid[pos.x, pos.y] = GridType.Corridor;
            newlyPainted.Add(pos);
        }

        if (_corridorWidth > 1)
            ExpandPath(path, newlyPainted);

        return true;
    }

    /// <summary>
    /// 経路の各セルから _corridorWidth の範囲（正方形）を Corridor に塗る。
    /// Door / Floor / Wall は保護して上書きしない。
    /// </summary>
    private void ExpandPath(List<Vector2Int> corridorPath, List<Vector2Int> newlyPainted)
    {
        int extraWidth = _corridorWidth - 1;

        var paintedSet = new HashSet<Vector2Int>(newlyPainted);
        for (int i = 0; i < corridorPath.Count; i++)
        {
            var center = corridorPath[i];
            if (!paintedSet.Contains(center)) continue;

            // 前後のセルとの差分から進行方向を求め、垂直軸を決定する。
            // 経路の端は隣接セルが 1 つしかないため、前後どちらかを代用する。
            var prev = (i > 0) ? corridorPath[i - 1] : corridorPath[i + 1];
            var next = (i < corridorPath.Count - 1) ? corridorPath[i + 1] : corridorPath[i - 1];
            var dir = next - prev;

            // 進行方向が X 軸方向（東西）なら垂直は Y 軸、Y 軸方向（南北）なら垂直は X 軸
            var perp = (dir.x != 0)
                ? new Vector2Int(0, 1)
                : new Vector2Int(1, 0);

            // 垂直方向へ extraWidth セル追加する（経路本体 + extraWidth = CorridorWidth）
            for (int w = 1; w <= extraWidth; w++)
            {
                PaintCell(center + perp * w);
            }
        }
    }

    private void PaintCell(Vector2Int pos)
    {
        if (!IsInGrid(pos)) return;
        var cellType = _grid[pos.x, pos.y];
        if (cellType == GridType.Door) return;
        if (cellType == GridType.Floor) return;
        if (cellType == GridType.Wall) return;
        _grid[pos.x, pos.y] = GridType.Corridor;
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

                if (cellType == GridType.Empty)
                {
                    foreach (var adjDir in neighbors)
                    {
                        var adj = neighbor + adjDir;
                        if (adj == current) continue;
                        if (!IsInGrid(adj)) continue;
                        if (_grid[adj.x, adj.y] == GridType.Corridor)
                        {
                            moveCost += 1.5f;
                            break;
                        }
                    }
                }


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