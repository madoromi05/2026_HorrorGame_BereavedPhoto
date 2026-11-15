using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector の凡例に使う、色だけの小さな四角を 1 つ描く係
/// RoomGridPainter と NavGridDrawer の凡例から呼ぶ。ラベルの文字は描かない
/// </summary>
internal static class ColorSwatch
{
    private const float kSize = 16f;	// 四角の一辺（px）

    public static void Draw(Color color)
    {
        var r = GUILayoutUtility.GetRect(kSize, kSize, GUILayout.Width(kSize), GUILayout.Height(kSize));
        EditorGUI.DrawRect(r, color);
    }
}
