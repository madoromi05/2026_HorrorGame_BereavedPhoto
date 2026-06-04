using DungeonSystem;
using Unity.AI.Navigation;
using UnityEngine;

/// <summary>
/// ダンジョン生成後に Door セルを走査し、
/// 部屋 NavMesh（Floor 側）と廊下 NavMesh（Corridor 側）を NavMeshLink で接続する。
/// DungeonGenerator と同じ GameObject にアタッチし、
/// Generate() 完了後に BuildLinks() を呼ぶこと。
/// </summary>
public class DungeonNavMeshLinker : MonoBehaviour
{
    private static readonly Vector2Int[] kDirs =
    {
        Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
    };

    public void BuildLinks(GridType[,] grid, float gridSize)
    {
        var parent = new GameObject("NavMeshLinks").transform;
        parent.SetParent(transform);

        int w = grid.GetLength(0);
        int h = grid.GetLength(1);

        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            if (grid[x, y] == GridType.Door)
                TryCreateLink(grid, x, y, gridSize, parent);
        }
    }

    private static void TryCreateLink(
        GridType[,] grid, int x, int y, float gridSize, Transform parent)
    {
        var center = new Vector3((x + 0.5f) * gridSize, 0f, (y + 0.5f) * gridSize);

        Vector3 roomOffset    = Vector3.zero;
        Vector3 corridorOffset = Vector3.zero;
        bool hasRoom     = false;
        bool hasCorridor = false;

        foreach (var d in kDirs)
        {
            int nx = x + d.x, ny = y + d.y;
            if (nx < 0 || nx >= grid.GetLength(0) || ny < 0 || ny >= grid.GetLength(1)) continue;

            var ct  = grid[nx, ny];
            // 隣接セルの中心へのオフセット（セル境界ではなく中心がNavMesh上に確実に乗る）
            var off = new Vector3(d.x * gridSize, 0f, d.y * gridSize);

            if ((ct == GridType.Floor || ct == GridType.PlayerPosition) && !hasRoom)
            {
                roomOffset = off;
                hasRoom    = true;
            }
            else if (ct == GridType.Corridor && !hasCorridor)
            {
                corridorOffset = off;
                hasCorridor    = true;
            }
        }

        if (!hasRoom || !hasCorridor) return;

        var go = new GameObject($"NavMeshLink_{x}_{y}");
        go.transform.SetParent(parent);
        go.transform.position = center;

        var link = go.AddComponent<NavMeshLink>();
        link.startPoint    = roomOffset;
        link.endPoint      = corridorOffset;
        link.width         = gridSize;
        link.bidirectional = true;
        link.autoUpdate    = false;
    }
}
