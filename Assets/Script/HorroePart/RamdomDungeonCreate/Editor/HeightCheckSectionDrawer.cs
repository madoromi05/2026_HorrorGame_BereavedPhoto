using UnityEditor;
using UnityEngine;

/// <summary>
/// Auto-fill の検知ボックス設定（Y 最小・Y 最大・XZ 係数）を Inspector で編集する係
/// 高さの断面図、入力欄、Scene View 表示の切り替えを描く。NavGridSectionDrawer が持ち、ナビグリッドのセクションの中から呼ぶ
/// Scene View への描画はしない。表示の切り替えは NavScenePreview.Enabled へ書くだけ
/// </summary>
internal class HeightCheckSectionDrawer
{
    private const float kDiagramWidth  = 80f;	// 高さダイアグラムに確保する幅
    private const float kDiagramHeight = 96f;	// 高さダイアグラムに確保する高さ

    private readonly System.Action   _repaint;	// 持ち主の Inspector を再描画する
    private readonly NavScenePreview _scenePreview;

    public HeightCheckSectionDrawer(System.Action repaint, NavScenePreview scenePreview)
    {
        _repaint      = repaint;
        _scenePreview = scenePreview;
    }

    public void Draw(RoomGridData data)
    {
        EditorGUILayout.LabelField("検知ボックス設定", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        Rect diagramArea = GUILayoutUtility.GetRect(kDiagramWidth, kDiagramHeight,	// 高さダイアグラム (左側に確保)
            GUILayout.Width(kDiagramWidth), GUILayout.Height(kDiagramHeight));
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
            _repaint();
        }

        EditorGUILayout.Space(2);
        bool newPreview = EditorGUILayout.Toggle("Scene View 表示", _scenePreview.Enabled);
        if (newPreview != _scenePreview.Enabled)
        {
            _scenePreview.Enabled = newPreview;
            SceneView.RepaintAll();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        if (_scenePreview.Enabled)
        {
            EditorGUILayout.HelpBox(
                "Prefab を Prefab Mode（ダブルクリック）で開くと\n" +
                "Scene View に検知ボックスとナビセルが表示されます。",
                MessageType.Info);
        }
    }

    /// <summary>
    /// Y 軸方向の断面図を diagramArea 内に描画し、検知範囲を視覚的に確認できるようにする
    /// ワールド Y 座標をピクセル座標へ変換する際は下端基準で反転する（画面は上が小さい Y）
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

        EditorGUI.DrawRect(	// 検知範囲の塗り
            new Rect(barRect.x, pxTop, barW, pxBot - pxTop),
            new Color(0.25f, 0.68f, 0.25f, 0.50f));

        EditorGUI.DrawRect(new Rect(barRect.x, pxFloor, barW, 1.5f),	// 床ライン
            new Color(0.65f, 0.45f, 0.15f, 0.90f));

        EditorGUI.DrawRect(new Rect(barRect.x, pxTop, barW, 1.5f),		// 検知上限ライン (水色)
            new Color(0.40f, 0.90f, 1.00f, 0.90f));

        EditorGUI.DrawRect(new Rect(barRect.x, pxBot, barW, 1.5f),		// 検知下限ライン (緑)
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

        var sY = new GUIStyle(s)	// バー左端に "Y" ラベル
        {
            fontSize  = 7,
            alignment = TextAnchor.UpperCenter,
            normal    = { textColor = new Color(0.55f, 0.55f, 0.55f) }
        };
        GUI.Label(new Rect(barRect.x, area.y, barW, 12), "Y", sY);
    }
}
