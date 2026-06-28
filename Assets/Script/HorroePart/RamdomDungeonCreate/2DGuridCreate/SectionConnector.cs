/// <summary>
/// セクション間を A* で接続する通路生成クラス。
/// MST（最小全域木）による全セクション接続と、追加分岐の生成を担当する。
/// MaxCorridorLength は「一直線に進める最大マス数」を意味し、
/// 超過した場合は自動的に曲がって迂回し、必ず接続する（スキップしない）。
/// 追加通路は MinExtraCorridorLength 条件を満たすセクションペアのみ生成する。
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

    // 全セクションペアを検査し、どちらの接続点から見ても MinExtraCorridorLength 以上
    // 離れているペアのみ追加通路を生成する。
    private void AddExtraBranches(FieldBluePrint bluePrint)
    {
        int minLength = bluePrint.MinExtraCorridorLength;

        for (int i = 0; i < _sections.Length; i++)
        {
            for (int j = i + 1; j < _sections.Length; j++)
            {
                var from = _sections[i];
                var to   = _sections[j];

                var startPos   = GetConnectionPoint(from, to);
                var endPos     = GetConnectionPoint(to, from);
                var fromCenter = from.GridPosition + from.GridSize / 2;
                var toCenter   = to.GridPosition   + to.GridSize   / 2;

                // from 側の接続点 → to の中心までの距離
                int distFromSide = Mathf.Abs(startPos.x - toCenter.x)
                                 + Mathf.Abs(startPos.y - toCenter.y);
                // to 側の接続点 → from の中心までの距離
                int distToSide   = Mathf.Abs(endPos.x - fromCenter.x)
                                 + Mathf.Abs(endPos.y - fromCenter.y);

                if (distFromSide >= minLength && distToSide >= minLength)
                    ConnectTwoSections(from, to);
            }
        }
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

        var path = _pathfinder.FindPath(_grid, startPos, endPos);
        if (path == null)
        {
            DebugCustom.LogWarning($"[SectionConnector] A* 失敗: {startPos} -> {endPos}");
            return false;
        }

        foreach (var pos in path)
        {
            var cellType = _grid[pos.x, pos.y];
            if (cellType == GridType.Door)     continue;
            if (cellType == GridType.Floor)    continue;
            if (cellType == GridType.Corridor) continue;
            _grid[pos.x, pos.y] = GridType.Corridor;
        }

        return true;
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
