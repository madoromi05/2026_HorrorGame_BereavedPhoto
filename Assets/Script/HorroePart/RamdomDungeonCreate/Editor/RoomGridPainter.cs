using UnityEditor;
using UnityEngine;

/// <summary>
/// ルームグリッド（Floor / Door / Wall / Player のセル種別）を Inspector で編集する係
/// Grid Size の入力と範囲外セルの刈り取り、凡例・クリックで種別が切り替わるグリッド・セル数・全リセットを描く
/// RoomGridDataEditor が持ち、OnInspectorGUI から呼ぶ。スクロール位置はこの係が持つ
/// ナビグリッドの表示・編集はしない（Grid Size 縮小で範囲外になったナビセルの刈り取りだけはここで行う）
/// </summary>
internal class RoomGridPainter
{
    // セルサイズとギャップを変えるとグリッド全体のスケールが一括で変わる
    private const float kCellSize = 36f;
    private const float kCellGap  = 2f;
    private const float kAxisSize = 20f;	// 軸ラベル列の幅
    private const float kMaxGridH = 400f;	// この高さを超えたらスクロールビューに切り替える

    // ---- Room Grid セル色 ----
    private static readonly Color kColorFloor  = new Color(0.20f, 0.65f, 0.20f);
    private static readonly Color kColorDoor   = new Color(1.00f, 0.85f, 0.00f);
    private static readonly Color kColorWall   = new Color(0.55f, 0.12f, 0.12f);
    private static readonly Color kColorPlayer = new Color(0.20f, 0.60f, 1.00f);
    private static readonly Color kColorBorder = new Color(0.08f, 0.08f, 0.08f);

    private readonly System.Action _repaint;	// 持ち主の Inspector を再描画する
    private Vector2 _dungeonScroll;

    public RoomGridPainter(System.Action repaint) => _repaint = repaint;

    public void DrawSizeField(RoomGridData data)
    {
        EditorGUI.BeginChangeCheck();
        var newSize = EditorGUILayout.Vector2IntField("Grid Size", data.GridSize);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(data, "Resize Room Grid");
            data.GridSize = new Vector2Int(Mathf.Max(1, newSize.x), Mathf.Max(1, newSize.y));
            PruneOutOfRangeCells(data);
            EditorUtility.SetDirty(data);
        }
    }

    public void DrawSection(RoomGridData data)
    {
        EditorGUILayout.LabelField("Room Grid", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            $"グリッドサイズ: {data.GridSize.x} × {data.GridSize.y}  " +
            "クリックで切り替え (Floor→Door→Wall→Player→Floor)",
            EditorStyles.miniLabel);
        DrawLegend();
        EditorGUILayout.Space(3);
        DrawGrid(data);
        EditorGUILayout.Space(3);

        int wc = data.WallPositions.Count, dc = data.DoorPositions.Count;
        int pc = data.PlayerPositions.Count;
        int fc = data.GridSize.x * data.GridSize.y - wc - dc;
        EditorGUILayout.LabelField(
            $"Floor: {fc}  Door: {dc}  Wall: {wc}  Player: {pc}  合計: {data.GridSize.x * data.GridSize.y}",
            EditorStyles.miniLabel);

        EditorGUILayout.Space(2);
        if (GUILayout.Button("全セルをFloorにリセット"))
        {
            Undo.RecordObject(data, "Reset Room Grid");
            data.DoorPositions.Clear();
            data.WallPositions.Clear();
            data.PlayerPositions.Clear();
            EditorUtility.SetDirty(data);
        }
    }

    private static void DrawLegend()
    {
        EditorGUILayout.BeginHorizontal();
        ColorSwatch.Draw(kColorFloor);  GUILayout.Label("Floor",  GUILayout.Width(44));
        ColorSwatch.Draw(kColorDoor);   GUILayout.Label("Door",   GUILayout.Width(44));
        ColorSwatch.Draw(kColorWall);   GUILayout.Label("Wall",   GUILayout.Width(44));
        ColorSwatch.Draw(kColorPlayer); GUILayout.Label("Player", GUILayout.Width(44));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawGrid(RoomGridData data)
    {
        float step   = kCellSize + kCellGap;
        float totalW = step * data.GridSize.x - kCellGap;
        float totalH = step * data.GridSize.y - kCellGap;
        float fullW  = totalW + kAxisSize;
        float fullH  = totalH + kAxisSize;

        bool scrollH = fullW > EditorGUIUtility.currentViewWidth - 30f;
        bool scrollV = fullH > kMaxGridH;
        if (scrollH || scrollV)
            _dungeonScroll = EditorGUILayout.BeginScrollView(
                _dungeonScroll, scrollH, scrollV,
                GUILayout.MaxHeight(scrollV ? kMaxGridH : fullH + 4));

        Rect full = GUILayoutUtility.GetRect(fullW, fullH);
        var  grid = new Rect(full.x + kAxisSize, full.y + kAxisSize, totalW, totalH);

        var axisStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 8,
            normal    = { textColor = new Color(0.72f, 0.72f, 0.72f) }
        };
        for (int x = 0; x < data.GridSize.x; x++)
            GUI.Label(new Rect(grid.x + x * step, full.y, kCellSize, kAxisSize - 2), x.ToString(), axisStyle);
        for (int y = 0; y < data.GridSize.y; y++)
            GUI.Label(new Rect(full.x, grid.y + y * step, kAxisSize - 2, kCellSize), y.ToString(), axisStyle);

        var cellStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 8,
            normal    = { textColor = Color.white }
        };

        for (int y = 0; y < data.GridSize.y; y++)
        {
            for (int x = 0; x < data.GridSize.x; x++)
            {
                var pos     = new Vector2Int(x, y);
                var cr      = new Rect(grid.x + x * step, grid.y + y * step, kCellSize, kCellSize);
                bool isDoor   = data.DoorPositions.Contains(pos);
                bool isWall   = data.WallPositions.Contains(pos);
                bool isPlayer = data.PlayerPositions.Contains(pos);

                Color bg  = isDoor ? kColorDoor : isWall ? kColorWall : isPlayer ? kColorPlayer : kColorFloor;
                string lbl = isDoor ? $"D\n{x},{y}" : isWall ? $"W\n{x},{y}" : isPlayer ? $"P\n{x},{y}" : $"F\n{x},{y}";

                EditorGUI.DrawRect(cr, kColorBorder);
                EditorGUI.DrawRect(new Rect(cr.x + 1, cr.y + 1, kCellSize - 2, kCellSize - 2), bg);
                GUI.Label(new Rect(cr.x + 1, cr.y + 1, kCellSize - 2, kCellSize - 2), lbl, cellStyle);

                if (Event.current.type == EventType.MouseDown &&
                    Event.current.button == 0 &&
                    cr.Contains(Event.current.mousePosition))
                {
                    Undo.RecordObject(data, "Edit Room Cell");
                    CycleCell(data, pos);
                    EditorUtility.SetDirty(data);
                    Event.current.Use();
                    _repaint();
                }
            }
        }

        if (scrollH || scrollV) EditorGUILayout.EndScrollView();
    }

    // Grid Size を縮小すると、旧サイズ基準で置かれたセルが新しい範囲外に取り残され、
    // Inspector 表示（現在の GridSize 内のみ描画）と実データが食い違ったまま残ってしまう
    // RoomGridPlacer も GridSize 内しか参照しないため範囲外セルは常に無効データとなるので、
    // リサイズの都度ここで刈り取り、保存されている値と実際に有効な値を一致させる
    private static void PruneOutOfRangeCells(RoomGridData data)
    {
        var size = data.GridSize;
        data.DoorPositions?.RemoveAll(p => !IsInRoomGrid(p, size));
        data.WallPositions?.RemoveAll(p => !IsInRoomGrid(p, size));
        data.PlayerPositions?.RemoveAll(p => !IsInRoomGrid(p, size));

        int sub = Mathf.Max(1, data.EnemyNavSubdivision);
        var navSize = new Vector2Int(size.x * sub, size.y * sub);
        data.EnemyNavWallCells?.RemoveAll(p => !IsInRoomGrid(p, navSize));
    }

    private static bool IsInRoomGrid(Vector2Int p, Vector2Int size)
        => p.x >= 0 && p.y >= 0 && p.x < size.x && p.y < size.y;

    // Floor を起点に一方向へ進む 4 段階サイクル
    // Player を最後に置くことで「通常セル(F/D/W)を設定してから必要な箇所だけ初期配置を指定」
    // という想定ワークフローに合わせている
    private static void CycleCell(RoomGridData data, Vector2Int pos)
    {
        bool isDoor   = data.DoorPositions.Contains(pos);
        bool isWall   = data.WallPositions.Contains(pos);
        bool isPlayer = data.PlayerPositions.Contains(pos);

        if      (!isDoor && !isWall && !isPlayer) { data.DoorPositions.Add(pos); }
        else if (isDoor)   { data.DoorPositions.Remove(pos);   data.WallPositions.Add(pos); }
        else if (isWall)   { data.WallPositions.Remove(pos);   data.PlayerPositions.Add(pos); }
        else               { data.PlayerPositions.Remove(pos); }
    }
}
