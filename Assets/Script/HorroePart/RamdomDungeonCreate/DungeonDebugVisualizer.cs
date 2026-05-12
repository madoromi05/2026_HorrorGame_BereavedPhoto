using DungeonSystem;
using UnityEngine;

/// <summary>
/// DebugMode 時のみ Grid セルと Section を色分けして可視化するクラス。
/// Grid 表示: Floor=緑, Wall=赤, Door=黄, Empty=灰(半透明)
/// Section 表示: インデックスごとに色を変えた半透明の床面。Start は白枠付き。
/// </summary>
public class DungeonDebugVisualizer
{
    private float _gridSize;

    // Grid セルの色
    private static readonly Color kFloorColor = new Color(0.1f, 0.9f, 0.1f, 0.75f);
    private static readonly Color kWallColor  = new Color(0.9f, 0.1f, 0.1f, 0.75f);
    private static readonly Color kDoorColor  = new Color(1.0f, 0.9f, 0.0f, 0.90f);
    private static readonly Color kEmptyColor = new Color(0.5f, 0.5f, 0.5f, 0.20f);

    // Section の色（インデックスで循環）
    private static readonly Color[] kSectionColors = new Color[]
    {
        new Color(1.0f, 0.3f, 0.3f, 0.25f),
        new Color(0.3f, 0.6f, 1.0f, 0.25f),
        new Color(0.3f, 1.0f, 0.5f, 0.25f),
        new Color(1.0f, 0.8f, 0.2f, 0.25f),
        new Color(0.8f, 0.3f, 1.0f, 0.25f),
        new Color(0.2f, 1.0f, 1.0f, 0.25f),
        new Color(1.0f, 0.5f, 0.1f, 0.25f),
        new Color(0.5f, 1.0f, 0.2f, 0.25f),
        new Color(0.2f, 0.4f, 1.0f, 0.25f),
    };

    public DungeonDebugVisualizer(float gridSize)
    {
        _gridSize = gridSize;
    }

    public void Visualize(GridType[,] grid, SectionData[] sections, Transform parent)
    {
        VisualizeSections(sections, parent);
        VisualizeGrid(grid, parent);
    }

    // ─── Section

    private void VisualizeSections(SectionData[] sections, Transform parent)
    {
        var sectionRoot = new GameObject("Debug_Sections").transform;
        sectionRoot.SetParent(parent);

        for (int i = 0; i < sections.Length; i++)
        {
            var section = sections[i];
            var color = kSectionColors[i % kSectionColors.Length];

            // 半透明の床面
            var center = new Vector3(
                (section.GridPosition.x + section.GridSize.x * 0.5f) * _gridSize,
                -0.05f,
                (section.GridPosition.y + section.GridSize.y * 0.5f) * _gridSize
            );
            var size = new Vector3(
                section.GridSize.x * _gridSize - 0.1f,
                0.02f,
                section.GridSize.y * _gridSize - 0.1f
            );
            var label = $"Section[{i}]_{section.Role}";
            CreateBox(center, size, color, label, sectionRoot);

            // Start セクションは白い外枠を追加
            if (section.Role == RoomType.Start)
                CreateSectionBorder(section, sectionRoot, i);
        }
    }

    private void CreateSectionBorder(SectionData section, Transform parent, int index)
    {
        var borderColor = new Color(1f, 1f, 1f, 0.8f);
        float t = 0.06f; // 枠の太さ（グリッド単位）
        float w = section.GridSize.x * _gridSize;
        float h = section.GridSize.y * _gridSize;
        float ox = section.GridPosition.x * _gridSize;
        float oz = section.GridPosition.y * _gridSize;
        float y = -0.04f;

        // 上下左右の細い棒 4 本で枠を構成
        CreateBox(new Vector3(ox + w * 0.5f, y, oz + t * 0.5f),          new Vector3(w, 0.04f, t), borderColor, $"Border_Top_{index}", parent);
        CreateBox(new Vector3(ox + w * 0.5f, y, oz + h - t * 0.5f),      new Vector3(w, 0.04f, t), borderColor, $"Border_Bot_{index}", parent);
        CreateBox(new Vector3(ox + t * 0.5f, y, oz + h * 0.5f),          new Vector3(t, 0.04f, h), borderColor, $"Border_L_{index}", parent);
        CreateBox(new Vector3(ox + w - t * 0.5f, y, oz + h * 0.5f),      new Vector3(t, 0.04f, h), borderColor, $"Border_R_{index}", parent);
    }

    // Grid 

    private void VisualizeGrid(GridType[,] grid, Transform parent)
    {
        var gridRoot = new GameObject("Debug_Grid").transform;
        gridRoot.SetParent(parent);

        var black = new Color(0f, 0f, 0f, 1f);
        float inner = _gridSize * 0.88f;  // 色付き部分のサイズ（黒枠の幅 = 6%ずつ）

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                var cellType = grid[x, y];
                var color = GetCellColor(cellType);
                var worldPos = new Vector3(
                            (x + 0.5f) * _gridSize,
                            0f,
                            (y + 0.5f) * _gridSize
                );
                // 色付き（少し小さく、上層）
                CreateBox(worldPos + Vector3.up * 0.01f, new Vector3(inner, 0.02f, inner), color, $"Cell_{cellType}_{x}_{y}", gridRoot);
            }
        }
    }

    private Color GetCellColor(GridType type) => type switch
    {
        GridType.Floor => kFloorColor,
        GridType.Wall  => kWallColor,
        GridType.Door  => kDoorColor,
        _              => kEmptyColor,
    };

    // ─── 共通プリミティブ生成 ────────────────────────────────────────────────────

    private void CreateBox(Vector3 position, Vector3 scale, Color color, string name, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = scale;

        var shader = FindTransparentShader();
        var mat = new Material(shader);
        ApplyColor(mat, color);
        go.GetComponent<Renderer>().material = mat;

        Object.Destroy(go.GetComponent<Collider>());
    }

    // URP → Built-in の順でシェーダーを探す
    private static Shader FindTransparentShader()
    {
        return Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Standard")
            ?? Shader.Find("Diffuse");
    }

    // 見つかったシェーダーがどのパイプラインか判別して色と透明度を設定する
    private static void ApplyColor(Material mat, Color color)
    {
        if (mat.HasProperty("_BaseColor"))
        {
            // URP / HDRP 系
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Surface", 1f);                                                      // 1 = Transparent
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHABLEND_ON");
        }
        else
        {
            // Built-in Standard
            mat.SetColor("_Color", color);
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        mat.renderQueue = 3000;
    }
}
