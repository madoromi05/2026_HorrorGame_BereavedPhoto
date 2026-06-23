/// <summary>
/// セクション間を A* で接続する通路生成クラス。
/// MST（最小全域木）による全セクション接続と、追加分岐の生成を担当する。
/// MaxCorridorLength は「一直線に進める最大マス数」を意味し、
/// 超過した場合は自動的に曲がって迂回し、必ず接続する（スキップしない）。
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
    private readonly AStarPathfinder _pathfinder = new AStarPathfinder();

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
            SectionData bestTo   = null;
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
                        minDist  = dist;
                        bestFrom = from;
                        bestTo   = to;
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
        var candidates = BuildAllPairs();

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

    // 追加分岐の候補として全セクションペアを返す。
    // 直線長制限は A* 内で処理するため、距離によるフィルタは不要。
    private List<(SectionData, SectionData)> BuildAllPairs()
    {
        var result = new List<(SectionData, SectionData)>();
        for (int i = 0; i < _sections.Length; i++)
            for (int j = i + 1; j < _sections.Length; j++)
                result.Add((_sections[i], _sections[j]));
        return result;
    }

    /// <summary>
    /// 2 セクション間を A* で接続し、経路上のセルを Corridor として書き込む。
    /// MaxCorridorLength を超える直線は A* 内で禁止されるため、
    /// 経路は自動的に曲がって既存通路に合流・分岐しながら必ず接続される。
    /// </summary>
    public bool ConnectTwoSections(SectionData from, SectionData to)
    {
        var startPos = GetConnectionPoint(from, to);
        var endPos   = GetConnectionPoint(to, from);

        var path = _pathfinder.FindPath(_grid, startPos, endPos, _maxCorridorLength);
        if (path == null)
        {
            DebugCustom.LogWarning($"[SectionConnector] A* 失敗: {startPos} -> {endPos}");
            return false;
        }

        var newlyPainted = new List<Vector2Int>();
        foreach (var pos in path)
        {
            var cellType = _grid[pos.x, pos.y];
            if (cellType == GridType.Door)     continue;
            if (cellType == GridType.Floor)    continue;
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

            var prev = (i > 0) ? corridorPath[i - 1] : corridorPath[i + 1];
            var next = (i < corridorPath.Count - 1) ? corridorPath[i + 1] : corridorPath[i - 1];
            var dir  = next - prev;

            var perp = (dir.x != 0)
                ? new Vector2Int(0, 1)
                : new Vector2Int(1, 0);

            for (int w = 1; w <= extraWidth; w++)
                PaintCell(center + perp * w);
        }
    }

    private void PaintCell(Vector2Int pos)
    {
        if (!IsInGrid(pos)) return;
        var cellType = _grid[pos.x, pos.y];
        if (cellType == GridType.Door)  return;
        if (cellType == GridType.Floor) return;
        if (cellType == GridType.Wall)  return;
        _grid[pos.x, pos.y] = GridType.Corridor;
    }

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

        var nearest  = positions[0];
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

    private bool IsInGrid(Vector2Int pos)
        => pos.x >= 0 && pos.x < _grid.GetLength(0)
        && pos.y >= 0 && pos.y < _grid.GetLength(1);
}
