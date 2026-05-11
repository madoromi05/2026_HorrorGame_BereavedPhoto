using DungeonSystem;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FieldBluePrintの情報をもとに、2Dのダンジョングリッドを構築するクラス
/// </summary>
public class DungeonGridBuilder
{
    private GridType[,] m_grid;
    private FieldBluePrint m_bluePrint;

    // 部屋あり区画 → グリッド上のDoor座標リスト
    private Dictionary<SectionData, List<Vector2Int>> m_sectionDoorMap;

    // 部屋なし区画 → 通路の中継点座標
    private Dictionary<SectionData, Vector2Int> m_pathPointMap;

    //部屋の設計図を受け取り、2次元グリッドを構築して返す
    public GridType[,] Build(FieldBluePrint bluePrint)
    {
        m_bluePrint = bluePrint;
        m_grid = new GridType[bluePrint.mapSize.x, bluePrint.mapSize.y];
        m_sectionDoorMap = new Dictionary<SectionData, List<Vector2Int>>();
        m_pathPointMap = new Dictionary<SectionData, Vector2Int>();

        PlaceRooms();

        return m_grid;
    }

    // 各区画に部屋 or 中継点を配置する
    private void PlaceRooms()
    {
        foreach (var section in m_bluePrint.sections)
        {
            if (section.roomGridData != null)
                PlaceRoomGrid(section);
            else
                PlacePath(section);
        }
    }

    // RoomGridDataをグリッドに書き込む
    private void PlaceRoomGrid(SectionData section)
    {
        var roomData = section.roomGridData;
        var doorPositions = new List<Vector2Int>();

        // 外周Wallを先に書き込む
        for (int x = -1; x <= roomData.size.x; x++)
        {
            for (int y = -1; y <= roomData.size.y; y++)
            {
                var worldPos = section.gridPosition + new Vector2Int(x, y);
                if (!IsInGrid(worldPos)) continue;
                // 部屋の内側は上書きしない
                if (x >= 0 && x < roomData.size.x && y >= 0 && y < roomData.size.y) continue;
                m_grid[worldPos.x, worldPos.y] = GridType.Wall;
            }
        }

        // 内部のFloorとDoorを配置
        for (int x = 0; x < roomData.size.x; x++)
        {
            for (int y = 0; y < roomData.size.y; y++)
            {
                var localPos = new Vector2Int(x, y);
                var worldPos = section.gridPosition + localPos;
                if (!IsInGrid(worldPos)) continue;

                if (roomData.doorPositions.Contains(localPos))
                {
                    m_grid[worldPos.x, worldPos.y] = GridType.Door;
                    doorPositions.Add(worldPos);
                }
                else
                {
                    m_grid[worldPos.x, worldPos.y] = GridType.Floor;
                }
            }
        }
        m_sectionDoorMap[section] = doorPositions;
    }

    // 部屋なし区画の中心を中継点として記録する
    private void PlacePath(SectionData section)
    {
        var center = section.gridPosition + section.gridSize / 2;
        m_pathPointMap[section] = center;
    }

    // 全区画を順番に接続する
    private void ConnectSections()
    {
        var sections = m_bluePrint.sections;
        var connectedSections = new HashSet<SectionData>();

        // 最初のSectionを接続済みとして開始
        connectedSections.Add(sections[0]);

        // 未接続Sectionがなくなるまで繰り返す
        while (connectedSections.Count < sections.Length)
        {
            SectionData bestFrom = null;
            SectionData bestTo = null;
            float minDist = float.MaxValue;

            // 接続済みと未接続の中から最近傍ペアを探す
            foreach (var connected in connectedSections)
            {
                foreach (var section in sections)
                {
                    if (connectedSections.Contains(section)) continue;

                    var connectedCenter = connected.gridPosition + connected.gridSize / 2;
                    var sectionCenter = section.gridPosition + section.gridSize / 2;
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

    // 余分な通路をランダムにN本追加する
    private void AddExtraBranches()
    {
        var sections = m_bluePrint.sections;
        int extraCount = Random.Range(
            m_bluePrint.minExtraBranchNum,
            m_bluePrint.maxExtraBranchNum + 1
        );

        for (int i = 0; i < extraCount; i++)
        {
            var from = sections[Random.Range(0, sections.Length)];
            var to = sections[Random.Range(0, sections.Length)];
            if (from == to) continue;
            ConnectTwoSections(from, to);
        }
    }
    // 2つのSectionを最近傍DoorもしくはPathで繋ぐ
    private void ConnectTwoSections(SectionData from, SectionData to)
    {
        var startPos = GetConnectionPoint(from, to);
        var endPos = GetConnectionPoint(to, from);

        var path = RunAStar(startPos, endPos);
        if (path == null) return;

        foreach (var pos in path)
        {
            // DoorとFloor（部屋内）は上書きしない
            if (m_grid[pos.x, pos.y] == GridType.Door) continue;
            if (m_grid[pos.x, pos.y] == GridType.Floor) continue;
            m_grid[pos.x, pos.y] = GridType.Floor;
        }
    }

    // SectionのDoor or Pathの中からtargetに最も近い座標を返す
    private Vector2Int GetConnectionPoint(SectionData section, SectionData target)
    {
        var targetCenter = target.gridPosition + target.gridSize / 2;

        if (m_sectionDoorMap.TryGetValue(section, out var doors))
            return FindNearest(doors, targetCenter);

        return m_pathPointMap[section];
    }

    // リストの中からtargetPosに最も近い座標を返す
    private Vector2Int FindNearest(List<Vector2Int> positions, Vector2Int targetPos)
    {
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

    // A*で start から end までの経路を返す
    // Wallのみ通行不可、それ以外は通行可
    private List<Vector2Int> RunAStar(Vector2Int start, Vector2Int end)
    {
        // コストと親座標の管理
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

        var neighbors = new[]
        {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right
        };

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
                if (m_grid[neighbor.x, neighbor.y] == GridType.Wall) continue;

                float moveCost = m_grid[neighbor.x, neighbor.y] switch
                {
                    GridType.Floor => 0.5f,
                    GridType.Door => 0.5f,
                    _ => 1.0f
                };

                float newG = gCost[current] + moveCost;
                if (gCost.TryGetValue(neighbor, out float existingG) && newG >= existingG) continue;

                gCost[neighbor] = newG;
                cameFrom[neighbor] = current;
                openSet.Add((newG + Heuristic(neighbor, end), neighbor));
            }
        }

        // 経路が見つからなかった場合
        return null;
    }

    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private List<Vector2Int> BuildPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        var path = new List<Vector2Int>();
        while (cameFrom.ContainsKey(current))
        {
            path.Add(current);
            current = cameFrom[current];
        }
        path.Reverse();
        return path;
    }

    private bool IsInGrid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < m_grid.GetLength(0)
            && pos.y >= 0 && pos.y < m_grid.GetLength(1);
    }
}