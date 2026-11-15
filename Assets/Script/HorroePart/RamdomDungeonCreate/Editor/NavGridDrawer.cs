using UnityEditor;
using UnityEngine;

/// <summary>
/// 敵ナビゲーショングリッドの凡例とセルの並びを Inspector に描き、クリックで手動ブロックを切り替える係
/// NavGridSectionDrawer が持ち、ナビグリッドのセクションの中から呼ぶ。スクロール位置はこの係が持つ
/// Subdivision・セル数の表示・Auto-fill は扱わない。自動ブロックと Door 外周の判定は NavCellClassifier に任せる
/// </summary>
internal class NavGridDrawer
{
    private const float kNavCellSize  = 10f;
    private const float kNavCellGap   = 1f;
    private const float kNavAxisSize  = 18f;
    private const float kNavGroupLine = 2f;		// ダンジョンセル境界線の太さ
    private const float kMaxNavH      = 420f;	// この高さを超えたらスクロールビューに切り替える

    // ---- Nav Grid セル色 ----
    // ユーザーが操作（クリック）できるのは Walkable ⇔ Manual の切り替えのみ
    // Door 外周と外周 Wall は自動判定のため変更不可
    private static readonly Color kColorNavWalkable   = new Color(0.20f, 0.75f, 0.20f, 0.85f);
    private static readonly Color kColorNavDoor       = new Color(0.95f, 0.78f, 0.00f, 0.90f);
    private static readonly Color kColorNavManual     = new Color(0.85f, 0.15f, 0.10f, 0.95f);
    private static readonly Color kColorNavAuto       = new Color(0.18f, 0.08f, 0.08f, 1.00f);
    private static readonly Color kColorNavCellBorder = new Color(0.04f, 0.04f, 0.04f);

    // ナビグリッド上でダンジョンセルの種別を示す視覚補助オーバーレイ（操作に影響しない）
    private static readonly Color kColorGroupWallBg   = new Color(0.55f, 0.05f, 0.05f, 0.40f);
    private static readonly Color kColorGroupDoorBg   = new Color(0.70f, 0.55f, 0.00f, 0.30f);
    private static readonly Color kColorGroupBorderLn = new Color(0.90f, 0.90f, 0.90f, 0.85f);

    private readonly System.Action _repaint;	// 持ち主の Inspector を再描画する
    private Vector2 _navScroll;

    public NavGridDrawer(System.Action repaint) => _repaint = repaint;

    public void DrawLegend()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("操作:", EditorStyles.miniLabel, GUILayout.Width(32));
        ColorSwatch.Draw(kColorNavWalkable); GUILayout.Label("通行可",   GUILayout.Width(52));
        ColorSwatch.Draw(kColorNavManual);   GUILayout.Label("ブロック", GUILayout.Width(52));
        GUILayout.Label("← クリックで切り替え", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("自動:", EditorStyles.miniLabel, GUILayout.Width(32));
        ColorSwatch.Draw(kColorNavDoor); GUILayout.Label("Door外周(通路)",     GUILayout.Width(88));
        ColorSwatch.Draw(kColorNavAuto); GUILayout.Label("外周Wall(変更不可)", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    public void DrawGrid(RoomGridData data, int navW, int navH)
    {
        int   sub    = data.EnemyNavSubdivision;
        float step   = kNavCellSize + kNavCellGap;
        float totalW = step * navW - kNavCellGap;
        float totalH = step * navH - kNavCellGap;
        float fullW  = totalW + kNavAxisSize;
        float fullH  = totalH + kNavAxisSize;

        bool scrollH = fullW > EditorGUIUtility.currentViewWidth - 30f;
        bool scrollV = fullH > kMaxNavH;
        if (scrollH || scrollV)
            _navScroll = EditorGUILayout.BeginScrollView(
                _navScroll, scrollH, scrollV,
                GUILayout.MaxHeight(scrollV ? kMaxNavH : fullH + 4));

        Rect full = GUILayoutUtility.GetRect(fullW, fullH);
        var  grid = new Rect(full.x + kNavAxisSize, full.y + kNavAxisSize, totalW, totalH);

        DrawGroupBackgrounds(data, grid, sub, step);
        DrawNavCells(data, grid, sub, step, navW, navH);
        DrawGroupBorders(data, grid, sub, step, totalW, totalH);
        DrawGroupLabels(data, grid, sub, step);
        DrawAxisLabels(data, full, grid, sub, step);

        if (scrollH || scrollV) EditorGUILayout.EndScrollView();
    }

    // 1. グループ背景
    private static void DrawGroupBackgrounds(RoomGridData data, Rect grid, int sub, float step)
    {
        for (int dy = 0; dy < data.GridSize.y; dy++)
        {
            for (int dx = 0; dx < data.GridSize.x; dx++)
            {
                var dpos    = new Vector2Int(dx, dy);
                bool isWall = data.WallPositions.Contains(dpos);
                bool isDoor = data.DoorPositions.Contains(dpos);
                if (!isWall && !isDoor) continue;
                float gw = sub * kNavCellSize + (sub - 1) * kNavCellGap;
                float gh = sub * kNavCellSize + (sub - 1) * kNavCellGap;
                EditorGUI.DrawRect(
                    new Rect(grid.x + dx * sub * step, grid.y + dy * sub * step, gw, gh),
                    isWall ? kColorGroupWallBg : kColorGroupDoorBg);
            }
        }
    }

    // 2. ナビセル
    private void DrawNavCells(RoomGridData data, Rect grid, int sub, float step, int navW, int navH)
    {
        for (int ny = 0; ny < navH; ny++)
        {
            for (int nx = 0; nx < navW; nx++)
            {
                var pos     = new Vector2Int(nx, ny);
                var cr      = new Rect(grid.x + nx * step, grid.y + ny * step, kNavCellSize, kNavCellSize);
                bool autoB  = NavCellClassifier.IsAutoBlocked(data, nx, ny, sub);
                bool isDoor = !autoB && NavCellClassifier.IsDoorBorder(data, nx, ny, sub);
                bool manB   = !autoB && !isDoor && data.EnemyNavWallCells.Contains(pos);

                Color cellCol = autoB  ? kColorNavAuto
                              : isDoor ? kColorNavDoor
                              : manB   ? kColorNavManual
                              :          kColorNavWalkable;

                EditorGUI.DrawRect(cr, kColorNavCellBorder);
                EditorGUI.DrawRect(new Rect(cr.x + 1, cr.y + 1, kNavCellSize - 2, kNavCellSize - 2), cellCol);

                if (!autoB &&	// autoB 以外はクリックで手動ブロック切替
                    Event.current.type == EventType.MouseDown &&
                    Event.current.button == 0 &&
                    cr.Contains(Event.current.mousePosition))
                {
                    Undo.RecordObject(data, "Edit Nav Cell");
                    if (manB) data.EnemyNavWallCells.Remove(pos);
                    else      data.EnemyNavWallCells.Add(pos);
                    EditorUtility.SetDirty(data);
                    Event.current.Use();
                    _repaint();
                }
            }
        }
    }

    // 3. ダンジョンセル境界線
    private static void DrawGroupBorders(RoomGridData data, Rect grid, int sub, float step, float totalW, float totalH)
    {
        for (int gx = 0; gx <= data.GridSize.x; gx++)
        {
            float lx = gx == 0               ? grid.x
                     : gx == data.GridSize.x ? grid.x + totalW - kNavGroupLine
                     :                          grid.x + gx * sub * step - kNavCellGap;
            EditorGUI.DrawRect(new Rect(lx, grid.y, kNavGroupLine, totalH), kColorGroupBorderLn);
        }
        for (int gy = 0; gy <= data.GridSize.y; gy++)
        {
            float ly = gy == 0               ? grid.y
                     : gy == data.GridSize.y ? grid.y + totalH - kNavGroupLine
                     :                          grid.y + gy * sub * step - kNavCellGap;
            EditorGUI.DrawRect(new Rect(grid.x, ly, totalW, kNavGroupLine), kColorGroupBorderLn);
        }
    }

    // 4. グループラベル
    private static void DrawGroupLabels(RoomGridData data, Rect grid, int sub, float step)
    {
        var groupStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize  = 7,
            normal    = { textColor = new Color(1f, 1f, 1f, 0.70f) }
        };
        for (int dy = 0; dy < data.GridSize.y; dy++)
        {
            for (int dx = 0; dx < data.GridSize.x; dx++)
            {
                var dpos = new Vector2Int(dx, dy);
                string t = data.WallPositions.Contains(dpos) ? $"({dx},{dy})W"
                         : data.DoorPositions.Contains(dpos) ? $"({dx},{dy})D"
                         :                                      $"({dx},{dy})";
                GUI.Label(
                    new Rect(grid.x + dx * sub * step + 1, grid.y + dy * sub * step + 1, sub * step, 11),
                    t, groupStyle);
            }
        }
    }

    // 5. 軸ラベル
    private static void DrawAxisLabels(RoomGridData data, Rect full, Rect grid, int sub, float step)
    {
        var axisStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 7,
            normal    = { textColor = new Color(0.68f, 0.68f, 0.68f) }
        };
        for (int dx = 0; dx < data.GridSize.x; dx++)
            GUI.Label(new Rect(grid.x + dx * sub * step, full.y, sub * step, kNavAxisSize), dx.ToString(), axisStyle);
        for (int dy = 0; dy < data.GridSize.y; dy++)
            GUI.Label(new Rect(full.x, grid.y + dy * sub * step, kNavAxisSize, sub * step), dy.ToString(), axisStyle);
    }
}
