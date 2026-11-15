/// <summary>
/// セクション間を A* で接続する通路生成クラス。
/// MST（最小全域木）による全セクション接続、追加分岐の生成、
/// 行き止まり（接続数1本）セクションへの補強接続を担当する。
/// FieldBluePrint.MaxStraightSightWorld から算出した直進マス上限を A* に渡し、
/// 上限を超える一直線は探索段階で曲げて迂回させる（超過時は自動で折れ、必ず接続する）。
/// 追加通路は MinExtraCorridorLength 条件を満たすセクションペアのみ生成する。
/// A* は avoidCorridorAdjacency を有効にして探索するため、
/// 既存通路とほぼ平行に隣接する新規通路（＝見た目上2列の通路）が作られにくくなる。
/// </summary>
using DungeonSystem;
using System.Collections.Generic;
using UnityEngine;

public class SectionConnector
{
    private SectionData[] _sections;
    private Dictionary<SectionData, List<Vector2Int>> _sectionDoorMap;
    private Dictionary<SectionData, Vector2Int> _pathPointMap;
    private Dictionary<SectionData, HashSet<SectionData>> _adjacency;
    private CorridorCarver _carver;

    /// <summary>
    /// MST で全セクションを接続したあと、追加分岐を生成する。
    /// 最後に、接続数が 1 本しかない（行き止まりになる）セクションへ
    /// 追加のループ通路を張って行き止まりを解消する。
    /// </summary>
    public void Connect(
        GridType[,] grid,
        SectionData[] sections,
        Dictionary<SectionData, List<Vector2Int>> sectionDoorMap,
        Dictionary<SectionData, Vector2Int> pathPointMap,
        FieldBluePrint bluePrint)
    {
        _sections = sections;
        _sectionDoorMap = sectionDoorMap;
        _pathPointMap = pathPointMap;
        _carver = new CorridorCarver(grid, bluePrint.MaxStraightCells);

        _adjacency = new Dictionary<SectionData, HashSet<SectionData>>();
        foreach (var section in sections)
            _adjacency[section] = new HashSet<SectionData>();

        ConnectAllSections();
        AddExtraBranches(bluePrint);
        EnsureMinimumDegree();
    }

    /// <summary>
    /// 接続数が 1 本以下のセクションを検出し、最も近い未接続のセクションへ
    /// 追加通路を張ることで行き止まりを解消する。
    /// </summary>
    private void EnsureMinimumDegree()
    {
        if (_sections.Length < 3) return;

        foreach (var section in _sections)
        {
            if (_adjacency[section].Count >= 2) continue;

            SectionData best = null;
            float minDist = float.MaxValue;

            foreach (var candidate in _sections)
            {
                if (candidate == section) continue;
                if (_adjacency[section].Contains(candidate)) continue;

                float dist = Vector2Int.Distance(section.Center, candidate.Center);

                if (dist < minDist)
                {
                    minDist = dist;
                    best = candidate;
                }
            }

            if (best != null)
                ConnectTwoSections(section, best);
        }
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

                    float dist = Vector2Int.Distance(from.Center, to.Center);

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
                var fromCenter = from.Center;
                var toCenter   = to.Center;

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
    /// 直進マス上限を超える直線は A* 内で禁止されるため、
    /// 経路は自動的に曲がって既存通路に合流・分岐しながら必ず接続される。
    /// </summary>
    public bool ConnectTwoSections(SectionData from, SectionData to)
    {
        var startPos = GetConnectionPoint(from, to);
        var endPos   = GetConnectionPoint(to, from);

        if (_carver.Carve(startPos, endPos) == null)
        {
            DebugCustom.LogWarning($"[SectionConnector] A* 失敗: {startPos} -> {endPos}");
            return false;
        }

        if (_adjacency != null)
        {
            if (_adjacency.TryGetValue(from, out var fromSet)) fromSet.Add(to);
            if (_adjacency.TryGetValue(to, out var toSet)) toSet.Add(from);
        }

        return true;
    }

    private Vector2Int GetConnectionPoint(SectionData section, SectionData target)
    {
        if (_sectionDoorMap.TryGetValue(section, out var doors))
            return FindNearestDoor(doors, target.Center);

        return _pathPointMap[section];
    }

    private Vector2Int FindNearestDoor(List<Vector2Int> doors, Vector2Int target)
    {
        if (doors == null || doors.Count == 0)
        {
            DebugCustom.LogWarning("[SectionConnector] FindNearestDoor: DoorPositions が空です。RoomGridData を確認してください。");
            return target;
        }

        return GridUtil.FindNearest(target, doors).Value;
    }
}
