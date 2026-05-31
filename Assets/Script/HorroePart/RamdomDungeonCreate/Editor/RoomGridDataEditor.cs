using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// RoomGridData の Inspector GUI。
/// グリッドセルをクリックして Floor / Door / Wall を切り替える。
/// 事前に敵の移動範囲度決めるためのクラス
/// </summary>
[CustomEditor(typeof(RoomGridData))]
public class RoomGridDataEditor : Editor
{
    private const float kCellSize = 36f;
    private const float kCellGap  = 3f;

    // ナビセル用の表示サイズ
    private const float kNavCellSize = 9f;
    private const float kNavCellGap  = 1f;

    // セルタイプの色
    private static readonly Color kColorFloor          = new Color(0.20f, 0.65f, 0.20f);
    private static readonly Color kColorDoor           = new Color(1.00f, 0.85f, 0.00f);
    private static readonly Color kColorWall           = new Color(0.55f, 0.12f, 0.12f);
    private static readonly Color kColorPlayerPosition = new Color(0.20f, 0.60f, 1.00f);
    private static readonly Color kColorBorder         = new Color(0.1f, 0.1f, 0.1f);

    // ナビグリッドの色
    private static readonly Color kColorNavWalkable   = new Color(0.25f, 0.80f, 0.25f, 0.6f);
    private static readonly Color kColorNavBlocked    = new Color(0.80f, 0.15f, 0.15f, 0.9f);
    // WallPositions から自動的に通行不可になるセル（クリック不可）
    private static readonly Color kColorNavAutoBlocked = new Color(0.30f, 0.20f, 0.20f, 1.0f);
    private static readonly Color kColorNavBorder     = new Color(0.05f, 0.05f, 0.05f);

    private bool _showNavGrid = true;

    public override void OnInspectorGUI()
    {
        var data = (RoomGridData)target;

        // GridSize フィールド
        EditorGUI.BeginChangeCheck();
        var newSize = EditorGUILayout.Vector2IntField("Grid Size", data.GridSize);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(data, "Resize Room Grid");
            data.GridSize = new Vector2Int(Mathf.Max(1, newSize.x), Mathf.Max(1, newSize.y));
            EditorUtility.SetDirty(data);
        }

        if (data.GridSize.x <= 0 || data.GridSize.y <= 0) return;

        // リストが null の場合は初期化
        if (data.DoorPositions == null) data.DoorPositions = new List<Vector2Int>();
        if (data.WallPositions == null) data.WallPositions = new List<Vector2Int>();
        if (data.PlayerPositions == null) data.PlayerPositions = new List<Vector2Int>();

        EditorGUILayout.Space(6);

        // 凡例
        DrawLegend();

        EditorGUILayout.Space(4);

        // グリッド描画
        DrawGrid(data);

        EditorGUILayout.Space(4);

        // 統計
        int wallCount  = data.WallPositions.Count;
        int doorCount  = data.DoorPositions.Count;
        int playerCount = data.PlayerPositions.Count;
        int floorCount = data.GridSize.x * data.GridSize.y - doorCount - wallCount;
        EditorGUILayout.LabelField($"Floor: {floorCount}   Door: {doorCount}   Wall: {wallCount}");

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
        _showNavGrid = EditorGUILayout.Foldout(_showNavGrid, "Enemy Navigation Grid", true);
        if (_showNavGrid)
            DrawNavGridSection(data);
    }

    private void DrawNavGridSection(RoomGridData data)
    {
        EditorGUI.BeginChangeCheck();
        var newSub = EditorGUILayout.IntSlider("Nav Subdivision", data.EnemyNavSubdivision, 1, 10);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(data, "Change Nav Subdivision");
            data.EnemyNavSubdivision = newSub;
            // 細分化数が変わった場合は既存のナビセルデータをリセットする
            data.EnemyNavWallCells.Clear();
            EditorUtility.SetDirty(data);
        }

        int sub  = data.EnemyNavSubdivision;
        int navW = data.GridSize.x * sub;
        int navH = data.GridSize.y * sub;

        if (navW <= 0 || navH <= 0) return;

        if (data.EnemyNavWallCells == null)
            data.EnemyNavWallCells = new List<Vector2Int>();

        // ナビグリッド凡例
        EditorGUILayout.BeginHorizontal();
        DrawColorBox(kColorNavWalkable);    EditorGUILayout.LabelField("通行可",       GUILayout.Width(52));
        DrawColorBox(kColorNavBlocked);     EditorGUILayout.LabelField("手動ブロック", GUILayout.Width(72));
        DrawColorBox(kColorNavAutoBlocked); EditorGUILayout.LabelField("自動(Wall)",   GUILayout.Width(64));
        EditorGUILayout.LabelField("← クリックで手動ブロック切替（自動は変更不可）");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"ナビグリッドサイズ: {navW} × {navH}",
            EditorStyles.miniLabel);
        EditorGUILayout.Space(2);

        DrawNavGrid(data, navW, navH);

        EditorGUILayout.Space(4);
        int manualBlocked = data.EnemyNavWallCells.Count;
        int autoBlocked   = CountAutoBlockedNavCells(data, sub);
        int total         = navW * navH;
        EditorGUILayout.LabelField(
            $"自動通行不可(WallPositions): {autoBlocked}   手動通行不可: {manualBlocked}   " +
            $"通行可: {total - autoBlocked - manualBlocked}   合計: {total}",
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
        {
            EditorGUILayout.LabelField(
                $"FieldBluePrint: {blueprint.name}  OneGridSize = {blueprint.OneGridSize}",
                EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "FieldBluePrint がプロジェクト内に見つかりません。先に作成してください。",
                MessageType.Warning);
        }

        using (new EditorGUI.DisabledScope(
            data.RoomPrefab == null || blueprint == null))
        {
            if (GUILayout.Button("Auto-fill: プレハブのコライダーからナビセルを自動設定"))
            {
                BakeNavGridFromPrefab(data, blueprint.OneGridSize);
                Repaint();
            }
        }

    }

    private void DrawNavGrid(RoomGridData data, int navW, int navH)
    {
        int sub   = data.EnemyNavSubdivision;
        float step   = kNavCellSize + kNavCellGap;
        float totalW = step * navW - kNavCellGap;
        float totalH = step * navH - kNavCellGap;

        Rect gridRect = GUILayoutUtility.GetRect(totalW, totalH);

        for (int ny = 0; ny < navH; ny++)
        {
            for (int nx = 0; nx < navW; nx++)
            {
                var pos = new Vector2Int(nx, ny);
                var cellRect = new Rect(
                    gridRect.x + nx * step,
                    gridRect.y + ny * step,
                    kNavCellSize,
                    kNavCellSize
                );

                // WallPositions に対応するダンジョンセルは自動的に通行不可
                bool autoBlocked   = IsNavCellAutoBlocked(data, nx, ny, sub);
                bool manualBlocked = !autoBlocked && data.EnemyNavWallCells.Contains(pos);

                EditorGUI.DrawRect(cellRect, kColorNavBorder);
                var inner = new Rect(cellRect.x + 1, cellRect.y + 1, cellRect.width - 2, cellRect.height - 2);

                Color cellColor = autoBlocked   ? kColorNavAutoBlocked
                                : manualBlocked ? kColorNavBlocked
                                                : kColorNavWalkable;
                EditorGUI.DrawRect(inner, cellColor);

                // 自動ブロックセルはクリック不可（WallPositions で決まるため）
                if (!autoBlocked &&
                    Event.current.type == EventType.MouseDown &&
                    Event.current.button == 0 &&
                    cellRect.Contains(Event.current.mousePosition))
                {
                    Undo.RecordObject(data, "Edit Nav Cell");
                    if (manualBlocked)
                        data.EnemyNavWallCells.Remove(pos);
                    else
                        data.EnemyNavWallCells.Add(pos);
                    EditorUtility.SetDirty(data);
                    Event.current.Use();
                    Repaint();
                }
            }
        }
    }

    /// <summary>
    /// ナビセル (nx, ny) が WallPositions に対応するダンジョンセル内かどうかを返す。
    /// 対応するダンジョンセル = (nx / sub, ny / sub)。
    /// </summary>
    private static bool IsNavCellAutoBlocked(RoomGridData data, int nx, int ny, int sub)
    {
        int dx = nx / sub;
        int dy = ny / sub;
        var dungeonCell = new Vector2Int(dx, dy);
        return data.WallPositions != null && data.WallPositions.Contains(dungeonCell);
    }

    /// <summary>自動ブロックされるナビセルの総数（WallPositions × sub²）を返す。</summary>
    private static int CountAutoBlockedNavCells(RoomGridData data, int sub)
    {
        if (data.WallPositions == null) return 0;
        return data.WallPositions.Count * sub * sub;
    }

    // ---- Auto-fill ----

    private static FieldBluePrint FindFieldBluePrint()
    {
        var guids = AssetDatabase.FindAssets("t:FieldBluePrint");
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<FieldBluePrint>(
            AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    /// <summary>
    /// Room Prefab のコライダーを走査し、ナビセルと重なるものを EnemyNavWallCells に設定する。
    ///
    /// 座標系: SectionPlacer は部屋の中心座標にプレハブを Instantiate するため、
    /// プレハブのローカル原点 = 部屋中心。ナビセル座標を中心基準に変換してから判定する。
    ///
    /// 検出高さ: 床コライダー（薄い水平面）を除外するため Y=0.3〜2.0m の範囲を使用する。
    /// isTrigger なコライダーはスキップする。
    /// WallPositions で既に自動ブロック済みのダンジョンセルに対応するナビセルはスキップする。
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

        int sub         = Mathf.Max(1, data.EnemyNavSubdivision);
        float navCellSize = dungeonCellSize / sub;
        int navW        = data.GridSize.x * sub;
        int navH        = data.GridSize.y * sub;

        // プレハブのローカル原点 = 部屋中心 なので、ナビセル座標を中心基準に変換する
        float roomCenterX = data.GridSize.x * dungeonCellSize * 0.5f;
        float roomCenterZ = data.GridSize.y * dungeonCellSize * 0.5f;

        // 判定に使うY範囲（床面の薄いコライダーを除外するため下限を 0.3f に設定）
        const float checkYCenter = 1.15f;
        const float checkYSize   = 1.7f;   // 0.3f 〜 2.0f の範囲

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
                    // WallPositions で既に自動ブロック済みのセルはスキップ
                    if (IsNavCellAutoBlocked(data, nx, ny, sub)) continue;

                    float localX = (nx + 0.5f) * navCellSize - roomCenterX;
                    float localZ = (ny + 0.5f) * navCellSize - roomCenterZ;

                    var cellBounds = new Bounds(
                        new Vector3(localX, checkYCenter, localZ),
                        new Vector3(navCellSize * 0.85f, checkYSize, navCellSize * 0.85f)
                    );

                    foreach (var col in colliders)
                    {
                        if (col.isTrigger) continue;
                        if (col.bounds.Intersects(cellBounds))
                        {
                            data.EnemyNavWallCells.Add(new Vector2Int(nx, ny));
                            blocked++;
                            break;
                        }
                    }
                }
            }

            EditorUtility.SetDirty(data);
            Debug.Log($"[RoomGridDataEditor] Auto-fill 完了: {blocked}/{navW * navH} セルをブロック ({data.name})");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }

    private void DrawLegend()
    {
        EditorGUILayout.BeginHorizontal();
        DrawColorBox(kColorFloor); EditorGUILayout.LabelField("Floor",  GUILayout.Width(48));
        DrawColorBox(kColorDoor);  EditorGUILayout.LabelField("Door",   GUILayout.Width(48));
        DrawColorBox(kColorWall);  EditorGUILayout.LabelField("Wall",   GUILayout.Width(48));
        DrawColorBox(kColorPlayerPosition); EditorGUILayout.LabelField("Player", GUILayout.Width(48));
        EditorGUILayout.LabelField("← クリックで切り替え");
        EditorGUILayout.EndHorizontal();
    }

    private void DrawColorBox(Color color)
    {
        var rect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16), GUILayout.Height(16));
        EditorGUI.DrawRect(rect, color);
    }

    private void DrawGrid(RoomGridData data)
    {
        float step = kCellSize + kCellGap;
        float totalW = step * data.GridSize.x - kCellGap;
        float totalH = step * data.GridSize.y - kCellGap;

        Rect gridRect = GUILayoutUtility.GetRect(totalW, totalH);

        var labelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 8,
            normal    = { textColor = Color.white }
        };

        for (int y = 0; y < data.GridSize.y; y++)
        {
            for (int x = 0; x < data.GridSize.x; x++)
            {
                var pos = new Vector2Int(x, y);
                var cellRect = new Rect(
                    gridRect.x + x * step,
                    gridRect.y + y * step,
                    kCellSize,
                    kCellSize
                );

                // 枠線（1px 内側に縮めて色セルを描く）
                EditorGUI.DrawRect(cellRect, kColorBorder);
                var innerRect = new Rect(cellRect.x + 1, cellRect.y + 1, cellRect.width - 2, cellRect.height - 2);

                if (data.DoorPositions.Contains(pos))
                {
                    EditorGUI.DrawRect(innerRect, kColorDoor);
                    GUI.Label(innerRect, $"{x},{y}\nD", labelStyle);
                }
                else if (data.WallPositions.Contains(pos))
                {
                    EditorGUI.DrawRect(innerRect, kColorWall);
                    GUI.Label(innerRect, $"{x},{y}\nW", labelStyle);
                }
                else if (data.PlayerPositions.Contains(pos))
                {
                    EditorGUI.DrawRect(innerRect, kColorPlayerPosition);
                    GUI.Label(innerRect, $"{x},{y}\nP", labelStyle);
                }
                else
                {
                    EditorGUI.DrawRect(innerRect, kColorFloor);
                    GUI.Label(innerRect, $"{x},{y}\nF", labelStyle);
                }

                // クリック処理
                if (Event.current.type == EventType.MouseDown &&
                    Event.current.button == 0 &&
                    cellRect.Contains(Event.current.mousePosition))
                {
                    Undo.RecordObject(data, "Edit Room Cell");
                    CycleCell(data, pos);
                    EditorUtility.SetDirty(data);
                    Event.current.Use();
                    Repaint();
                }
            }
        }
    }

    /// <summary>
    /// Floor → Door → Wall → Floor の順に切り替える
    /// </summary>
    private static void CycleCell(RoomGridData data, Vector2Int pos)
    {
        bool isDoor = data.DoorPositions.Contains(pos);
        bool isWall = data.WallPositions.Contains(pos);
        bool isPlayer = data.PlayerPositions.Contains(pos);

        if (!isDoor && !isWall && !isPlayer)
        {
            // Floor → Door
            data.DoorPositions.Add(pos);
        }
        else if (isDoor)
        {
            // Door → Wall
            data.DoorPositions.Remove(pos);
            data.WallPositions.Add(pos);
        }
        else
        {
            // Wall → Floor
            data.WallPositions.Remove(pos);
        }
    }
}
