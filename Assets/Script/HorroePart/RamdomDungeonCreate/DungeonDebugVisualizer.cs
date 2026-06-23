using DungeonSystem;
using UnityEngine;

/// <summary>
/// DebugMode 時のみ Grid セルと Section を Gizmos で色分けして可視化するクラス。
/// Grid 表示: Floor=緑, Wall=赤, Door=黄, Corridor=水色, Empty=暗灰
/// Section 表示: インデックスごとに色を変えた床面。Start は白枠付き。
/// Scene ビューの Gizmos ボタンを ON にすること。
/// </summary>
public class DungeonDebugVisualizer
{
    private float _gridSize;
    private DungeonGizmoDrawer _drawer;
    private const float kSectionOffsetY = -5f;

    private static readonly Color kFloorColor          = new Color(0.1f, 0.9f, 0.1f, 0.85f);
    private static readonly Color kWallColor           = new Color(0.9f, 0.1f, 0.1f, 0.85f);
    private static readonly Color kDoorColor           = new Color(1.0f, 0.9f, 0.0f, 0.95f);
    private static readonly Color kEmptyColor          = new Color(0.3f, 0.3f, 0.3f, 0.25f);
    private static readonly Color kCorridorColor       = new Color(0.2f, 0.6f, 1.0f, 0.85f);
    private static readonly Color kPlayerPositionColor = new Color(1.0f, 1.0f, 1.0f, 1.00f);

    private static readonly Color[] kSectionColors = new Color[]
    {
        new Color(1.0f, 0.3f, 0.3f, 0.35f),
        new Color(0.3f, 0.6f, 1.0f, 0.35f),
        new Color(0.3f, 1.0f, 0.5f, 0.35f),
        new Color(1.0f, 0.8f, 0.2f, 0.35f),
        new Color(0.8f, 0.3f, 1.0f, 0.35f),
        new Color(0.2f, 1.0f, 1.0f, 0.35f),
        new Color(1.0f, 0.5f, 0.1f, 0.35f),
        new Color(0.5f, 1.0f, 0.2f, 0.35f),
        new Color(0.2f, 0.4f, 1.0f, 0.35f),
    };

    public DungeonDebugVisualizer(float gridSize)
    {
        _gridSize = gridSize;
    }

    public void Visualize(GridType[,] grid, SectionData[] sections, Transform parent)
    {
        _drawer = parent.gameObject.AddComponent<DungeonGizmoDrawer>();
        VisualizeSections(sections);
        VisualizeGrid(grid);
    }

    private void VisualizeSections(SectionData[] sections)
    {
        for (int i = 0; i < sections.Length; i++)
        {
            var section = sections[i];
            var color   = kSectionColors[i % kSectionColors.Length];

            var center = new Vector3(
                (section.GridPosition.x + section.GridSize.x * 0.5f) * _gridSize,
                kSectionOffsetY,
                (section.GridPosition.y + section.GridSize.y * 0.5f) * _gridSize
            );
            var size = new Vector3(
                section.GridSize.x * _gridSize - 0.1f,
                0.1f,
                section.GridSize.y * _gridSize - 0.1f
            );
            _drawer.AddBox(center, size, color);

            if (section.Role == RoomType.Start)
                DrawSectionBorder(section);
        }
    }

    private void DrawSectionBorder(SectionData section)
    {
        var borderColor = new Color(1f, 1f, 1f, 0.9f);
        float t  = 0.5f;
        float w  = section.GridSize.x * _gridSize;
        float h  = section.GridSize.y * _gridSize;
        float ox = section.GridPosition.x * _gridSize;
        float oz = section.GridPosition.y * _gridSize;
        float y  = kSectionOffsetY;

        _drawer.AddBox(new Vector3(ox + w * 0.5f,     y, oz + t * 0.5f),     new Vector3(w, 0.15f, t), borderColor);
        _drawer.AddBox(new Vector3(ox + w * 0.5f,     y, oz + h - t * 0.5f), new Vector3(w, 0.15f, t), borderColor);
        _drawer.AddBox(new Vector3(ox + t * 0.5f,     y, oz + h * 0.5f),     new Vector3(t, 0.15f, h), borderColor);
        _drawer.AddBox(new Vector3(ox + w - t * 0.5f, y, oz + h * 0.5f),     new Vector3(t, 0.15f, h), borderColor);
    }

    private void VisualizeGrid(GridType[,] grid)
    {
        float inner = _gridSize * 0.88f;

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                var color    = GetCellColor(grid[x, y]);
                var worldPos = new Vector3(
                    (x + 0.5f) * _gridSize,
                    -0.5f,
                    (y + 0.5f) * _gridSize
                );
                _drawer.AddBox(worldPos, new Vector3(inner, 0.05f, inner), color);
            }
        }
    }

    private Color GetCellColor(GridType type) => type switch
    {
        GridType.Floor          => kFloorColor,
        GridType.Corridor       => kCorridorColor,
        GridType.Wall           => kWallColor,
        GridType.Door           => kDoorColor,
        GridType.PlayerPosition => kPlayerPositionColor,
        _                       => kEmptyColor,
    };
}
