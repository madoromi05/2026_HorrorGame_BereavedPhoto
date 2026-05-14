using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// RoomGridData の Inspector GUI。
/// グリッドセルをクリックして Floor / Door / Wall を切り替える。
///   Floor (緑) : 通行可能な室内セル
///   Door  (黄) : 壁の開口部（通路接続点）FBXの開口部と座標を合わせること
///   Wall  (赤) : 通行不可の壁セル（FBXの壁メッシュと座標を合わせること）
/// </summary>
[CustomEditor(typeof(RoomGridData))]
public class RoomGridDataEditor : Editor
{
    private const float kCellSize = 36f;
    private const float kCellGap  = 3f;

    // セルタイプの色
    private static readonly Color kColorFloor = new Color(0.20f, 0.65f, 0.20f);
    private static readonly Color kColorDoor  = new Color(1.00f, 0.85f, 0.00f);
    private static readonly Color kColorWall  = new Color(0.55f, 0.12f, 0.12f);
    private static readonly Color kColorPlayerPosition = new Color(0.20f, 0.60f, 1.00f);
    private static readonly Color kColorBorder = new Color(0.1f, 0.1f, 0.1f);

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
