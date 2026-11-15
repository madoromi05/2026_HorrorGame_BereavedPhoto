using UnityEngine;

/// <summary>
/// ナビセルが「自動ブロック（外周 Wall）」か「Door 外周（通路出口）」かを判定する係
/// Inspector のナビグリッド・Scene View のプレビュー・Auto-fill の焼き込みが同じ判定を使うため、ここ 1 か所に置く
/// 手動ブロック（EnemyNavWallCells）の判定、描画、データの書き込みはしない
/// </summary>
internal static class NavCellClassifier
{
    /// <summary>
    /// グリッドの外周かつ対応ダンジョンセルが Door でないセルを自動ブロックと判定する
    /// 外周は壁に埋まっている前提だが、Door セルの外周は通路出口なので除外する
    /// </summary>
    public static bool IsAutoBlocked(RoomGridData data, int nx, int ny, int sub)
    {
        int navW = data.GridSize.x * sub;
        int navH = data.GridSize.y * sub;
        if (nx != 0 && ny != 0 && nx != navW - 1 && ny != navH - 1) return false;

        var dungeonCell = new Vector2Int(nx / sub, ny / sub);
        return data.DoorPositions == null || !data.DoorPositions.Contains(dungeonCell);
    }

    /// <summary>IsAutoBlocked の Door 判定部分を単独で公開したもの。凡例や色分けで再利用する</summary>
    public static bool IsDoorBorder(RoomGridData data, int nx, int ny, int sub)
    {
        int navW = data.GridSize.x * sub;
        int navH = data.GridSize.y * sub;
        if (nx != 0 && ny != 0 && nx != navW - 1 && ny != navH - 1) return false;

        var dungeonCell = new Vector2Int(nx / sub, ny / sub);
        return data.DoorPositions != null && data.DoorPositions.Contains(dungeonCell);
    }

    // 境界条件の解析的な計算が Door 除外で複雑になるため、全セルを走査して数える
    // ナビグリッドの外周のみが対象なので実コストは O(W+H)
    public static int CountAutoBlocked(RoomGridData data, int sub)
    {
        int navW = data.GridSize.x * sub;
        int navH = data.GridSize.y * sub;
        if (navW <= 0 || navH <= 0) return 0;
        int count = 0;
        for (int ny = 0; ny < navH; ny++)
            for (int nx = 0; nx < navW; nx++)
                if (IsAutoBlocked(data, nx, ny, sub)) count++;
        return count;
    }
}
