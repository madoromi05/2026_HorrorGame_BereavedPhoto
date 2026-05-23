/// <summary>
/// SectionData をもとに部屋・パスポイントをグリッドへ書き込むクラス。
/// グリッドへの書き込みと、後工程が必要とする接続情報の生成のみを担当する。
/// </summary>
using DungeonSystem;
using System.Collections.Generic;
using UnityEngine;

public class RoomGridPlacer
{
    /// <summary>
    /// 全セクションをグリッドに書き込み、接続情報を返す。
    /// </summary>
    public void Place(
        GridType[,] grid,
        SectionData[] sections,
        out Dictionary<SectionData, List<Vector2Int>> sectionDoorMap,
        out Dictionary<SectionData, Vector2Int> pathPointMap)
    {
        sectionDoorMap = new Dictionary<SectionData, List<Vector2Int>>();
        pathPointMap = new Dictionary<SectionData, Vector2Int>();

        foreach (var section in sections)
        {
            if (section.RoomGridData != null)
                PlaceRoomGrid(grid, section, sectionDoorMap);
            else
                PlacePath(section, pathPointMap);
        }
    }

    /// <summary>
    /// RoomGridData のセル情報をグリッドに書き込む。
    /// セクション内でランダムオフセットを設定し、壁との間に 1 セルのマージンを確保する。
    /// </summary>
    private void PlaceRoomGrid(
        GridType[,] grid,
        SectionData section,
        Dictionary<SectionData, List<Vector2Int>> sectionDoorMap)
    {
        var roomData = section.RoomGridData;
        var doorPositions = new List<Vector2Int>();

        // 部屋がセクション端に張り付かないよう 1 セル以上の余白を確保する
        int spaceX = section.GridSize.x - roomData.GridSize.x;
        int spaceY = section.GridSize.y - roomData.GridSize.y;
        int offsetX = spaceX >= 2 ? Random.Range(1, spaceX) : 0;
        int offsetY = spaceY >= 2 ? Random.Range(1, spaceY) : 0;
        section.RoomGridPosition = section.GridPosition + new Vector2Int(offsetX, offsetY);

        for (int x = 0; x < roomData.GridSize.x; x++)
        {
            for (int y = 0; y < roomData.GridSize.y; y++)
            {
                var localPos = new Vector2Int(x, y);
                var worldPos = section.RoomGridPosition + localPos;
                if (!IsInGrid(grid, worldPos)) continue;

                grid[worldPos.x, worldPos.y] = ResolveCellType(roomData, localPos);

                if (roomData.DoorPositions.Contains(localPos))
                    doorPositions.Add(worldPos);
            }
        }

        sectionDoorMap[section] = doorPositions;
    }

    /// <summary>
    /// ローカル座標のセルタイプを RoomGridData から解決する。
    /// 優先順位: Door > Wall > PlayerPosition > Floor
    /// </summary>
    private GridType ResolveCellType(RoomGridData roomData, Vector2Int localPos)
    {
        if (roomData.DoorPositions.Contains(localPos)) return GridType.Door;
        if (roomData.WallPositions != null
            && roomData.WallPositions.Contains(localPos)) return GridType.Wall;
        if (roomData.PlayerPositions != null
            && roomData.PlayerPositions.Contains(localPos)) return GridType.PlayerPosition;
        return GridType.Floor;
    }

    /// <summary>
    /// 部屋を持たないセクションの中心点を通路接続の起点として登録する。
    /// </summary>
    private void PlacePath(SectionData section, Dictionary<SectionData, Vector2Int> pathPointMap)
    {
        pathPointMap[section] = section.GridPosition + section.GridSize / 2;
    }

    private bool IsInGrid(GridType[,] grid, Vector2Int pos)
        => pos.x >= 0 && pos.x < grid.GetLength(0)
        && pos.y >= 0 && pos.y < grid.GetLength(1);
}