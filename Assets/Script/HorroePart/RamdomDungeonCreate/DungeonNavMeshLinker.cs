using DungeonSystem;
using Unity.AI.Navigation;
using UnityEngine;

/// <summary>
/// ダンジョン生成後に Door セルを走査し、
/// 部屋 NavMesh（Floor 側）と廊下 NavMesh（Corridor 側）を NavMeshLink で接続する
/// DungeonGenerator と同じ GameObject にアタッチし、
/// Generate() 完了後に BuildLinks() を呼ぶこと
/// </summary>
public class DungeonNavMeshLinker : MonoBehaviour
{
    public void BuildLinks(GridType[,] grid, float gridSize)
    {
        var parent = new GameObject("NavMeshLinks").transform;
        parent.SetParent(transform);

        foreach (var door in GridUtil.CellsOf(grid, GridType.Door))
            TryCreateLink(grid, door, gridSize, parent);
    }

    private static void TryCreateLink(
        GridType[,] grid, Vector2Int door, float gridSize, Transform parent)
    {
        if (!TryFindLinkEnds(grid, door, gridSize, out var roomOffset, out var corridorOffset)) return;
        CreateLink(GridUtil.CellCenterToWorld(door, gridSize, 0f), roomOffset, corridorOffset, door, gridSize, parent);
    }

    /// <summary>
    /// ドアの 4 方向から、最初に見つかった部屋側と廊下側のセル中心へのオフセットを返す。どちらかが無ければ false
    /// </summary>
    private static bool TryFindLinkEnds(GridType[,] grid, Vector2Int door, float gridSize,
                                        out Vector3 roomOffset, out Vector3 corridorOffset)
    {
        roomOffset     = Vector3.zero;
        corridorOffset = Vector3.zero;
        bool hasRoom     = false;
        bool hasCorridor = false;

        foreach (var d in GridUtil.ClockwiseDirs)
        {
            var n = door + d;
            if (!GridUtil.IsInGrid(grid, n)) continue;

            var ct  = grid[n.x, n.y];
            var off = new Vector3(d.x * gridSize, 0f, d.y * gridSize);	// 隣接セルの中心へのオフセット（セル境界ではなく中心がNavMesh上に確実に乗る）

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

        return hasRoom && hasCorridor;
    }

    private static void CreateLink(Vector3 center, Vector3 roomOffset, Vector3 corridorOffset,
                                   Vector2Int door, float gridSize, Transform parent)
    {
        var go = new GameObject($"NavMeshLink_{door.x}_{door.y}");
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
