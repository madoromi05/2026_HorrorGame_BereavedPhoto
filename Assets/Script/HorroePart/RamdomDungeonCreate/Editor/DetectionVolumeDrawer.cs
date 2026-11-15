using UnityEditor;
using UnityEngine;

/// <summary>
/// Auto-fill で使用するコライダー検知ボックス全体を Scene View に半透明で描く係
/// 上下面と 4 側面を DrawSolidRectangleWithOutline で描画し、床（Y=0）との関係をラインと高さのラベルで補完する
/// NavScenePreview から呼ぶ。ナビセルやダンジョンセルの線は描かない
/// </summary>
internal static class DetectionVolumeDrawer
{
    private static readonly Color kSceneBoxFill   = new Color(0.40f, 0.80f, 1.00f, 0.06f);
    private static readonly Color kSceneBoxBorder = new Color(0.40f, 0.80f, 1.00f, 0.80f);
    private static readonly Color kSceneYMinLine  = new Color(0.40f, 1.00f, 0.40f, 0.90f);
    private static readonly Color kSceneYMaxLine  = new Color(0.40f, 0.90f, 1.00f, 0.90f);
    private static readonly Color kSceneFloorLine = new Color(0.75f, 0.50f, 0.20f, 0.80f);

    public static void Draw(float halfW, float halfD, float yMin, float yMax)
    {
        DrawVolume(halfW, halfD, yMin, yMax);
        DrawHeightLabels(halfW, halfD, yMin, yMax);
    }

    private static void DrawVolume(float halfW, float halfD, float yMin, float yMax)
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

    private static void DrawHeightLabels(float halfW, float halfD, float yMin, float yMax)
    {
        var labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            normal   = { textColor = Color.white }
        };
        Handles.Label(new Vector3(-halfW, yMax + 0.08f, -halfD), $"  ▲ 検知上限  Y = {yMax:F2}", labelStyle);
        Handles.Label(new Vector3(-halfW, yMin - 0.20f, -halfD), $"  ▼ 検知下限  Y = {yMin:F2}", labelStyle);
        Handles.Label(new Vector3(-halfW, 0.08f,        -halfD), "  床 Y = 0",                   labelStyle);
    }
}
