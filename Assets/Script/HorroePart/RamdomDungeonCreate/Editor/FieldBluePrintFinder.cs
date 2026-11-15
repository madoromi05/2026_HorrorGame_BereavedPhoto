using UnityEditor;

/// <summary>
/// プロジェクト内の FieldBluePrint アセットを探す係
/// NavGridSectionDrawer（Auto-fill の表示）と NavScenePreview（Scene View の描画）から呼ぶ。見つかった値を持ち続けない
/// </summary>
internal static class FieldBluePrintFinder
{
    public static FieldBluePrint Find()
    {
        var guids = AssetDatabase.FindAssets("t:FieldBluePrint");
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<FieldBluePrint>(
            AssetDatabase.GUIDToAssetPath(guids[0]));
    }
}
