using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// RoomGridData の Inspector カスタムエディタ。
/// ルームグリッドのセル種別編集、敵ナビゲーションセルの手動設定、
/// および Auto-fill（プレハブコライダーからの自動生成）を提供する。
/// Prefab Mode 中は Scene View に検知ボックスとナビセルを Gizmo 表示する。
/// </summary>
[CustomEditor(typeof(RoomGridData))]
public class RoomGridDataEditor : Editor
{
    // セルサイズとギャップを変えるとグリッド全体のスケールが一括で変わる。
    private const float kCellSize    = 36f;
    private const float kCellGap     = 2f;
    private const float kNavCellSize = 10f;
    private const float kNavCellGap  = 1f;

    private const float kAxisSize    = 20f; // 軸ラベル列の幅
    private const float kNavAxisSize = 18f;
    private const float kNavGroupLine = 2f; // ダンジョンセル境界線の太さ

    // この高さを超えたらスクロールビューに切り替える。
    private const float kMaxGridH = 400f;
    private const float kMaxNavH  = 420f;

    // ---- Room Grid セル色 ----
    private static readonly Color kColorFloor  = new Color(0.20f, 0.65f, 0.20f);
    private static readonly Color kColorDoor   = new Color(1.00f, 0.85f, 0.00f);
    private static readonly Color kColorWall   = new Color(0.55f, 0.12f, 0.12f);
    private static readonly Color kColorPlayer = new Color(0.20f, 0.60f, 1.00f);
    private static readonly Color kColorBorder = new Color(0.08f, 0.08f, 0.08f);

    // ---- Nav Grid セル色 ----
    // ユーザーが操作（クリック）できるのは Walkable ⇔ Manual の切り替えのみ。
    // Door 外周と外周 Wall は自動判定のため変更不可。
    private static readonly Color kColorNavWalkable   = new Color(0.20f, 0.75f, 0.20f, 0.85f);
    private static readonly Color kColorNavDoor       = new Color(0.95f, 0.78f, 0.00f, 0.90f);
    private static readonly Color kColorNavManual     = new Color(0.85f, 0.15f, 0.10f, 0.95f);
    private static readonly Color kColorNavAuto       = new Color(0.18f, 0.08f, 0.08f, 1.00f);
    private static readonly Color kColorNavCellBorder = new Color(0.04f, 0.04f, 0.04f);

    // ナビグリッド上でダンジョンセルの種別を示す視覚補助オーバーレイ（操作に影響しない）。
    private static readonly Color kColorGroupWallBg   = new Color(0.55f, 0.05f, 0.05f, 0.40f);
    private static readonly Color kColorGroupDoorBg   = new Color(0.70f, 0.55f, 0.00f, 0.30f);
    private static readonly Color kColorGroupBorderLn = new Color(0.90f, 0.90f, 0.90f, 0.85f);

    // ---- Scene View Gizmo 色 ----
    private static readonly Color kSceneNavWalkable = new Color(0.10f, 0.80f, 0.20f, 0.18f);
    private static readonly Color kSceneNavDoor     = new Color(0.95f, 0.78f, 0.00f, 0.50f);
    private static readonly Color kSceneNavManual   = new Color(0.90f, 0.40f, 0.05f, 0.40f);
    private static readonly Color kSceneNavAuto     = new Color(0.80f, 0.10f, 0.10f, 0.40f);
    private static readonly Color kSceneBoxFill     = new Color(0.40f, 0.80f, 1.00f, 0.06f);
    private static readonly Color kSceneBoxBorder   = new Color(0.40f, 0.80f, 1.00f, 0.80f);
    private static readonly Color kSceneYMinLine    = new Color(0.40f, 1.00f, 0.40f, 0.90f);
    private static readonly Color kSceneYMaxLine    = new Color(0.40f, 0.90f, 1.00f, 0.90f);
    private static readonly Color kSceneFloorLine   = new Color(0.75f, 0.50f, 0.20f, 0.80f);
    private static readonly Color kSceneDungeonGrid = new Color(1.00f, 1.00f, 1.00f, 0.35f);

    private bool    _showNavGrid      = true;
    private bool    _showScenePreview = true;
    private Vector2 _dungeonScroll;
    private Vector2 _navScroll;

    // SceneView のデリゲートはエディタが非選択になっても登録が残るため、
    // OnDisable で確実に解除しないと他のオブジェクト選択時にも描画が走り続ける。
    private void OnEnable()  => SceneView.duringSceneGui += DrawSceneGUI;
    private void OnDisable() => SceneView.duringSceneGui -= DrawSceneGUI;

    public override void OnInspectorGUI()
    {
        var data = (RoomGridData)target;

        EditorGUI.BeginChangeCheck();
        var newSize = EditorGUILayout.Vector2IntField("Grid Size", data.GridSize);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(data, "Resize Room Grid");
            data.GridSize = new Vector2Int(Mathf.Max(1, newSize.x), Mathf.Max(1, newSize.y));
            EditorUtility.SetDirty(data);
        }

        if (data.GridSize.x <= 0 || data.GridSize.y <= 0) return;

        if (data.DoorPositions   == null) data.DoorPositions   = new List<Vector2Int>();
        if (data.WallPositions   == null) data.WallPositions   = new List<Vector2Int>();
        if (data.PlayerPositions == null) data.PlayerPositions = new List<Vector2Int>();

        EditorGUILayout.Space(6);

        // ---- Room Grid ----
        EditorGUILayout.LabelField("Room Grid", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            $"グリッドサイズ: {data.GridSize.x} × {data.GridSize.y}  " +
            "クリックで切り替え (Floor→Door→Wall→Player→Floor)",
            EditorStyles.miniLabel);
        DrawRoomLegend();
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

        // ---- Enemy Navigation Grid ----
        EditorGUILayout.Space(10);
        _showNavGrid = EditorGUILayout.Foldout(_showNavGrid, "Enemy Navigation Grid", true, EditorStyles.foldoutHeader);
        if (_showNavGrid)
            DrawNavGridSection(data);
    }

    // ---- Room Grid ----

    private static void DrawRoomLegend()
    {
        EditorGUILayout.BeginHorizontal();
        ColorSwatch(kColorFloor);  GUILayout.Label("Floor",  GUILayout.Width(44));
        ColorSwatch(kColorDoor);   GUILayout.Label("Door",   GUILayout.Width(44));
        ColorSwatch(kColorWall);   GUILayout.Label("Wall",   GUILayout.Width(44));
        ColorSwatch(kColorPlayer); GUILayout.Label("Player", GUILayout.Width(44));
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
                    Repaint();
                }
            }
        }

        if (scrollH || scrollV) EditorGUILayout.EndScrollView();
    }

    // Floor を起点に一方向へ進む 4 段階サイクル。
    // Player を最後に置くことで「通常セル(F/D/W)を設定してから必要な箇所だけ初期配置を指定」
    // という想定ワークフローに合わせている。
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

    // ---- Nav Grid ----

    private void DrawNavGridSection(RoomGridData data)
    {
        // ---- Subdivision ----
        EditorGUI.BeginChangeCheck();
        var newSub = EditorGUILayout.IntSlider("Nav Subdivision", data.EnemyNavSubdivision, 1, 10);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(data, "Change Nav Subdivision");
            data.EnemyNavSubdivision = newSub;
            data.EnemyNavWallCells.Clear();
            EditorUtility.SetDirty(data);
        }

        int sub  = data.EnemyNavSubdivision;
        int navW = data.GridSize.x * sub;
        int navH = data.GridSize.y * sub;
        if (navW <= 0 || navH <= 0) return;
        if (data.EnemyNavWallCells == null) data.EnemyNavWallCells = new List<Vector2Int>();

        EditorGUILayout.LabelField(
            $"解析範囲: {navW} × {navH} ナビセル  " +
            $"= ルームグリッド {data.GridSize.x}×{data.GridSize.y}  ×  subdivision {sub}",
            EditorStyles.miniLabel);

        EditorGUILayout.Space(6);
        DrawHeightCheckSection(data);

        // ---- ナビグリッド表示 ----
        EditorGUILayout.Space(6);
        DrawNavLegend();
        EditorGUILayout.Space(3);
        DrawNavGrid(data, navW, navH);
        EditorGUILayout.Space(4);

        int autoB    = CountAutoBlockedNavCells(data, sub);
        int manualB  = data.EnemyNavWallCells.Count;
        int total    = navW * navH;
        int walkable = total - autoB - manualB;
        EditorGUILayout.LabelField(
            $"通行可: {walkable}  手動ブロック: {manualB}  自動(Wall): {autoB}  合計: {total}",
            EditorStyles.miniLabel);

        EditorGUILayout.Space(2);
        if (GUILayout.Button("ナビグリッドを全て通行可にリセット"))
        {
            Undo.RecordObject(data, "Reset Nav Grid");
            data.EnemyNavWallCells.Clear();
            EditorUtility.SetDirty(data);
        }

        // ---- Auto-fill ----
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Auto-fill", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        var newPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Room Prefab", data.RoomPrefab, typeof(GameObject), false);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(data, "Set Room Prefab");
            data.RoomPrefab = newPrefab;
            EditorUtility.SetDirty(data);
        }

        var blueprint = FindFieldBluePrint();
        if (blueprint != null)
            EditorGUILayout.LabelField(
                $"FieldBluePrint: {blueprint.name}  OneGridSize = {blueprint.OneGridSize}",
                EditorStyles.miniLabel);
        else
            EditorGUILayout.HelpBox(
                "FieldBluePrint がプロジェクト内に見つかりません。先に作成してください。",
                MessageType.Warning);

        using (new EditorGUI.DisabledScope(data.RoomPrefab == null || blueprint == null))
        {
            if (GUILayout.Button("Auto-fill: プレハブのコライダーからナビセルを自動設定"))
            {
                BakeNavGridFromPrefab(data, blueprint.OneGridSize);
                Repaint();
            }
        }
    }

    private void DrawHeightCheckSection(RoomGridData data)
    {
        EditorGUILayout.LabelField("検知ボックス設定", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        // 高さダイアグラム (左 80px 確保)
        Rect diagramArea = GUILayoutUtility.GetRect(80f, 96f,
            GUILayout.Width(80f), GUILayout.Height(96f));
        if (Event.current.type == EventType.Repaint)
            DrawHeightDiagramRect(diagramArea, data.NavCheckYMin, data.NavCheckYMax);

        GUILayout.Space(4);

        // スライダー (右側)
        EditorGUILayout.BeginVertical();
        EditorGUI.BeginChangeCheck();

        float newMin   = EditorGUILayout.FloatField("Y 最小", data.NavCheckYMin);
        float newMax   = EditorGUILayout.FloatField("Y 最大", data.NavCheckYMax);
        float newScale = EditorGUILayout.Slider("XZ 係数", data.NavCheckXZScale, 0.5f, 1.0f);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(data, "Change NavCheck Params");
            data.NavCheckYMin    = Mathf.Min(newMin, newMax - 0.01f);
            data.NavCheckYMax    = Mathf.Max(newMax, newMin + 0.01f);
            data.NavCheckXZScale = newScale;
            EditorUtility.SetDirty(data);
            Repaint();
        }

        EditorGUILayout.Space(2);
        bool newPreview = EditorGUILayout.Toggle("Scene View 表示", _showScenePreview);
        if (newPreview != _showScenePreview)
        {
            _showScenePreview = newPreview;
            SceneView.RepaintAll();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        if (_showScenePreview)
        {
            EditorGUILayout.HelpBox(
                "Prefab を Prefab Mode（ダブルクリック）で開くと\n" +
                "Scene View に検知ボックスとナビセルが表示されます。",
                MessageType.Info);
        }
    }

    /// <summary>
    /// Y 軸方向の断面図を diagramArea 内に描画し、検知範囲を視覚的に確認できるようにする。
    /// ワールド Y 座標をピクセル座標へ変換する際は下端基準で反転する（画面は上が小さい Y）。
    /// </summary>
    private static void DrawHeightDiagramRect(Rect area, float yMin, float yMax)
    {
        const float kDispMin = -0.8f;
        const float kDispMax =  3.8f;
        float dispRange = kDispMax - kDispMin;
        float barW      = 26f;
        float lx        = area.x + barW + 3f;
        float lw        = area.width - barW - 3f;

        var barRect = new Rect(area.x, area.y, barW, area.height);
        EditorGUI.DrawRect(barRect, new Color(0.10f, 0.10f, 0.10f));

        // -- ピクセル Y 変換 --
        float ToPx(float worldY)
            => area.yMax - (worldY - kDispMin) / dispRange * area.height;

        float pxTop   = Mathf.Clamp(ToPx(yMax), area.y, area.yMax);
        float pxBot   = Mathf.Clamp(ToPx(yMin), area.y, area.yMax);
        float pxFloor = Mathf.Clamp(ToPx(0f),   area.y, area.yMax);

        // 検知範囲の塗り
        EditorGUI.DrawRect(
            new Rect(barRect.x, pxTop, barW, pxBot - pxTop),
            new Color(0.25f, 0.68f, 0.25f, 0.50f));

        // 床ライン
        EditorGUI.DrawRect(new Rect(barRect.x, pxFloor, barW, 1.5f),
            new Color(0.65f, 0.45f, 0.15f, 0.90f));

        // 検知上限ライン (水色)
        EditorGUI.DrawRect(new Rect(barRect.x, pxTop, barW, 1.5f),
            new Color(0.40f, 0.90f, 1.00f, 0.90f));

        // 検知下限ライン (緑)
        EditorGUI.DrawRect(new Rect(barRect.x, pxBot, barW, 1.5f),
            new Color(0.40f, 1.00f, 0.40f, 0.90f));

        // ラベル
        var s = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize  = 6,
            alignment = TextAnchor.MiddleLeft,
            normal    = { textColor = new Color(0.85f, 0.85f, 0.85f) }
        };
        var sFloor = new GUIStyle(s)
        {
            normal = { textColor = new Color(0.65f, 0.45f, 0.15f) }
        };
        var sTop = new GUIStyle(s)
        {
            normal = { textColor = new Color(0.40f, 0.90f, 1.00f) }
        };
        var sBot = new GUIStyle(s)
        {
            normal = { textColor = new Color(0.40f, 1.00f, 0.40f) }
        };

        GUI.Label(new Rect(lx, pxTop  - 5f, lw, 10f), $"↑{yMax:F2}", sTop);
        GUI.Label(new Rect(lx, pxBot  - 5f, lw, 10f), $"↓{yMin:F2}", sBot);
        GUI.Label(new Rect(lx, pxFloor - 5f, lw, 10f), "床 Y=0",     sFloor);

        // バー左端に "Y" ラベル
        var sY = new GUIStyle(s)
        {
            fontSize  = 7,
            alignment = TextAnchor.UpperCenter,
            normal    = { textColor = new Color(0.55f, 0.55f, 0.55f) }
        };
        GUI.Label(new Rect(barRect.x, area.y, barW, 12), "Y", sY);
    }

    private static void DrawNavLegend()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("操作:", EditorStyles.miniLabel, GUILayout.Width(32));
        ColorSwatch(kColorNavWalkable); GUILayout.Label("通行可",   GUILayout.Width(52));
        ColorSwatch(kColorNavManual);   GUILayout.Label("ブロック", GUILayout.Width(52));
        GUILayout.Label("← クリックで切り替え", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("自動:", EditorStyles.miniLabel, GUILayout.Width(32));
        ColorSwatch(kColorNavDoor); GUILayout.Label("Door外周(通路)",     GUILayout.Width(88));
        ColorSwatch(kColorNavAuto); GUILayout.Label("外周Wall(変更不可)", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawNavGrid(RoomGridData data, int navW, int navH)
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

        // 1. グループ背景
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

        // 2. ナビセル
        for (int ny = 0; ny < navH; ny++)
        {
            for (int nx = 0; nx < navW; nx++)
            {
                var pos     = new Vector2Int(nx, ny);
                var cr      = new Rect(grid.x + nx * step, grid.y + ny * step, kNavCellSize, kNavCellSize);
                bool autoB  = IsNavCellAutoBlocked(data, nx, ny, sub);
                bool isDoor = !autoB && IsNavCellDoorBorder(data, nx, ny, sub);
                bool manB   = !autoB && !isDoor && data.EnemyNavWallCells.Contains(pos);

                Color cellCol = autoB  ? kColorNavAuto
                              : isDoor ? kColorNavDoor
                              : manB   ? kColorNavManual
                              :          kColorNavWalkable;

                EditorGUI.DrawRect(cr, kColorNavCellBorder);
                EditorGUI.DrawRect(new Rect(cr.x + 1, cr.y + 1, kNavCellSize - 2, kNavCellSize - 2), cellCol);

                // autoB 以外はクリックで手動ブロック切替
                if (!autoB &&
                    Event.current.type == EventType.MouseDown &&
                    Event.current.button == 0 &&
                    cr.Contains(Event.current.mousePosition))
                {
                    Undo.RecordObject(data, "Edit Nav Cell");
                    if (manB) data.EnemyNavWallCells.Remove(pos);
                    else      data.EnemyNavWallCells.Add(pos);
                    EditorUtility.SetDirty(data);
                    Event.current.Use();
                    Repaint();
                }
            }
        }

        // 3. ダンジョンセル境界線
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

        // 4. グループラベル
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

        // 5. 軸ラベル
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

        if (scrollH || scrollV) EditorGUILayout.EndScrollView();
    }

    // 現在 Prefab Mode で開いているプレハブが RoomPrefab と一致する場合のみ描画する。
    // 一致しない状態で描画すると無関係な Scene に重なってしまうため。
    private void DrawSceneGUI(SceneView sceneView)
    {
        if (!_showScenePreview) return;

        var data = (RoomGridData)target;
        if (data?.RoomPrefab == null) return;

        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage == null) return;
        if (stage.assetPath != AssetDatabase.GetAssetPath(data.RoomPrefab)) return;

        var bp = FindFieldBluePrint();
        if (bp == null) return;

        Handles.matrix = Matrix4x4.identity;
        DrawNavPreviewInScene(data, bp.OneGridSize);
        sceneView.Repaint();
    }

    private static void DrawNavPreviewInScene(RoomGridData data, float dungeonCellSize)
    {
        int   sub         = data.EnemyNavSubdivision;
        float navCellSize = dungeonCellSize / sub;
        int   navW        = data.GridSize.x * sub;
        int   navH        = data.GridSize.y * sub;
        float halfW       = data.GridSize.x * dungeonCellSize * 0.5f;
        float halfD       = data.GridSize.y * dungeonCellSize * 0.5f;
        float yMin        = data.NavCheckYMin;
        float yMax        = data.NavCheckYMax;

        // hs を navCellSize 未満にすることでセル境界線を自然に表現している。
        float hs = navCellSize * 0.5f * 0.88f;
        for (int ny = 0; ny < navH; ny++)
        {
            for (int nx = 0; nx < navW; nx++)
            {
                bool autoB  = IsNavCellAutoBlocked(data, nx, ny, sub);
                bool isDoor = !autoB && IsNavCellDoorBorder(data, nx, ny, sub);
                bool manB   = !autoB && !isDoor && data.EnemyNavWallCells != null
                              && data.EnemyNavWallCells.Contains(new Vector2Int(nx, ny));

                Color fc = autoB  ? kSceneNavAuto
                         : isDoor ? kSceneNavDoor
                         : manB   ? kSceneNavManual
                         :          kSceneNavWalkable;
                float cx = (nx + 0.5f) * navCellSize - halfW;
                float cz = (ny + 0.5f) * navCellSize - halfD;

                Handles.color = fc;
                Handles.DrawAAConvexPolygon(
                    new Vector3(cx - hs, 0.01f, cz - hs),
                    new Vector3(cx + hs, 0.01f, cz - hs),
                    new Vector3(cx + hs, 0.01f, cz + hs),
                    new Vector3(cx - hs, 0.01f, cz + hs));
            }
        }

        Handles.color = kSceneDungeonGrid;
        for (int gx = 0; gx <= data.GridSize.x; gx++)
        {
            float lx = gx * dungeonCellSize - halfW;
            Handles.DrawAAPolyLine(2f,
                new Vector3(lx, 0.02f, -halfD),
                new Vector3(lx, 0.02f,  halfD));
        }
        for (int gz = 0; gz <= data.GridSize.y; gz++)
        {
            float lz = gz * dungeonCellSize - halfD;
            Handles.DrawAAPolyLine(2f,
                new Vector3(-halfW, 0.02f, lz),
                new Vector3( halfW, 0.02f, lz));
        }

        DrawDetectionVolume(halfW, halfD, yMin, yMax);

        var labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            normal   = { textColor = Color.white }
        };
        Handles.Label(new Vector3(-halfW, yMax + 0.08f, -halfD), $"  ▲ 検知上限  Y = {yMax:F2}", labelStyle);
        Handles.Label(new Vector3(-halfW, yMin - 0.20f, -halfD), $"  ▼ 検知下限  Y = {yMin:F2}", labelStyle);
        Handles.Label(new Vector3(-halfW, 0.08f,        -halfD), "  床 Y = 0",                   labelStyle);
    }

    /// <summary>
    /// Auto-fill で使用するコライダー検知ボックス全体を半透明で可視化する。
    /// 上下面と 4 側面を DrawSolidRectangleWithOutline で描画し、
    /// 床（Y=0）との関係をラインで補完している。
    /// </summary>
    private static void DrawDetectionVolume(float halfW, float halfD, float yMin, float yMax)
    {
        var bot = new Vector3[]
        {
            new Vector3(-halfW, yMin, -halfD), new Vector3( halfW, yMin, -halfD),
            new Vector3( halfW, yMin,  halfD), new Vector3(-halfW, yMin,  halfD),
        };
        var top = new Vector3[]
        {
            new Vector3(-halfW, yMax, -halfD), new Vector3( halfW, yMax, -halfD),
            new Vector3( halfW, yMax,  halfD), new Vector3(-halfW, yMax,  halfD),
        };
        Handles.DrawSolidRectangleWithOutline(bot, kSceneBoxFill, kSceneYMinLine);
        Handles.DrawSolidRectangleWithOutline(top, kSceneBoxFill, kSceneYMaxLine);

        Handles.color = kSceneBoxBorder;
        float[][] corners = { new[] { -halfW, -halfD }, new[] { halfW, -halfD },
                              new[] {  halfW,  halfD }, new[] { -halfW,  halfD } };
        foreach (var c in corners)
            Handles.DrawAAPolyLine(1.5f,
                new Vector3(c[0], yMin, c[1]),
                new Vector3(c[0], yMax, c[1]));

        var sides = new[]
        {
            new[] { bot[0], bot[1], top[1], top[0] },
            new[] { bot[1], bot[2], top[2], top[1] },
            new[] { bot[2], bot[3], top[3], top[2] },
            new[] { bot[3], bot[0], top[0], top[3] },
        };
        foreach (var s in sides)
            Handles.DrawSolidRectangleWithOutline(s, kSceneBoxFill, Color.clear);

        Handles.color = kSceneFloorLine;
        float[] xs = { -halfW, halfW, halfW, -halfW, -halfW };
        float[] zs = { -halfD, -halfD, halfD, halfD, -halfD };
        for (int i = 0; i < 4; i++)
            Handles.DrawAAPolyLine(2f,
                new Vector3(xs[i],   0f, zs[i]),
                new Vector3(xs[i+1], 0f, zs[i+1]));
    }

    private static void ColorSwatch(Color color)
    {
        var r = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16), GUILayout.Height(16));
        EditorGUI.DrawRect(r, color);
    }

    /// <summary>
    /// グリッドの外周かつ対応ダンジョンセルが Door でないセルを自動ブロックと判定する。
    /// 外周は壁に埋まっている前提だが、Door セルの外周は通路出口なので除外する。
    /// </summary>
    private static bool IsNavCellAutoBlocked(RoomGridData data, int nx, int ny, int sub)
    {
        int navW = data.GridSize.x * sub;
        int navH = data.GridSize.y * sub;
        if (nx != 0 && ny != 0 && nx != navW - 1 && ny != navH - 1) return false;

        var dungeonCell = new Vector2Int(nx / sub, ny / sub);
        return data.DoorPositions == null || !data.DoorPositions.Contains(dungeonCell);
    }

    /// <summary>IsNavCellAutoBlocked の Door 判定部分を単独で公開したもの。凡例や色分けで再利用する。</summary>
    private static bool IsNavCellDoorBorder(RoomGridData data, int nx, int ny, int sub)
    {
        int navW = data.GridSize.x * sub;
        int navH = data.GridSize.y * sub;
        if (nx != 0 && ny != 0 && nx != navW - 1 && ny != navH - 1) return false;

        var dungeonCell = new Vector2Int(nx / sub, ny / sub);
        return data.DoorPositions != null && data.DoorPositions.Contains(dungeonCell);
    }

    // 境界条件の解析的な計算が Door 除外で複雑になるため、全セルを走査して数える。
    // ナビグリッドの外周のみが対象なので実コストは O(W+H)。
    private static int CountAutoBlockedNavCells(RoomGridData data, int sub)
    {
        int navW = data.GridSize.x * sub;
        int navH = data.GridSize.y * sub;
        if (navW <= 0 || navH <= 0) return 0;
        int count = 0;
        for (int ny = 0; ny < navH; ny++)
            for (int nx = 0; nx < navW; nx++)
                if (IsNavCellAutoBlocked(data, nx, ny, sub)) count++;
        return count;
    }

    private static FieldBluePrint FindFieldBluePrint()
    {
        var guids = AssetDatabase.FindAssets("t:FieldBluePrint");
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<FieldBluePrint>(
            AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    /// <summary>
    /// プレハブのコライダー情報を走査して EnemyNavWallCells を自動生成する。
    /// プレハブのローカル原点は部屋中心に置かれている（SectionPlacer の配置規約）ため、
    /// ナビセルのグリッド座標を中心基準のローカル座標に変換してから Bounds 判定を行う。
    /// isTrigger コライダーはナビゲーション障害物ではないのでスキップする。
    /// </summary>
    private static void BakeNavGridFromPrefab(RoomGridData data, float dungeonCellSize)
    {
        var path = AssetDatabase.GetAssetPath(data.RoomPrefab);
        if (string.IsNullOrEmpty(path))
        {
            EditorUtility.DisplayDialog("Auto-fill エラー",
                "プレハブのパスが取得できませんでした。\nAssets 以下に保存されたプレハブを指定してください。", "OK");
            return;
        }

        int   sub          = Mathf.Max(1, data.EnemyNavSubdivision);
        float navCellSize  = dungeonCellSize / sub;
        int   navW         = data.GridSize.x * sub;
        int   navH         = data.GridSize.y * sub;
        float roomCenterX  = data.GridSize.x * dungeonCellSize * 0.5f;
        float roomCenterZ  = data.GridSize.y * dungeonCellSize * 0.5f;

        float checkYCenter = (data.NavCheckYMin + data.NavCheckYMax) * 0.5f;
        float checkYSize   = data.NavCheckYMax - data.NavCheckYMin;
        float xzScale      = data.NavCheckXZScale;

        var prefabContents = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var colliders = prefabContents.GetComponentsInChildren<Collider>(false);
            Undo.RecordObject(data, "Auto-fill Nav Grid from Prefab");
            data.EnemyNavWallCells.Clear();

            int blocked = 0;
            for (int ny = 0; ny < navH; ny++)
            {
                for (int nx = 0; nx < navW; nx++)
                {
                    if (IsNavCellAutoBlocked(data, nx, ny, sub)) continue;

                    float localX = (nx + 0.5f) * navCellSize - roomCenterX;
                    float localZ = (ny + 0.5f) * navCellSize - roomCenterZ;

                    var bounds = new Bounds(
                        new Vector3(localX, checkYCenter, localZ),
                        new Vector3(navCellSize * xzScale, checkYSize, navCellSize * xzScale));

                    foreach (var col in colliders)
                    {
                        if (col.isTrigger) continue;
                        if (col.bounds.Intersects(bounds))
                        {
                            data.EnemyNavWallCells.Add(new Vector2Int(nx, ny));
                            blocked++;
                            break;
                        }
                    }
                }
            }

            EditorUtility.SetDirty(data);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }
}
