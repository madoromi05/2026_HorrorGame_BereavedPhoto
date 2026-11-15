using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Prefab Mode で開いた部屋プレハブの上に、ナビセルとダンジョンセルの線を Scene View へ描く係
/// 表示の on/off（Enabled）もこの係が持つ。RoomGridDataEditor が持ち、SceneView.duringSceneGui から呼ぶ
/// 検知ボックスは DetectionVolumeDrawer に任せる。データの書き換えはしない
/// </summary>
internal class NavScenePreview
{
    private const float kCellFillRatio = 0.88f;	// 塗りの一辺をナビセルより小さくする比率
    private const float kCellY         = 0.01f;	// ナビセルを描く高さ（床のわずかに上）
    private const float kGridLineY     = 0.02f;	// ダンジョンセルの線を描く高さ（ナビセルより上）
    private const float kGridLineWidth = 2f;	// ダンジョンセルの線の太さ

    private static readonly Color kSceneNavWalkable = new Color(0.10f, 0.80f, 0.20f, 0.18f);
    private static readonly Color kSceneNavDoor     = new Color(0.95f, 0.78f, 0.00f, 0.50f);
    private static readonly Color kSceneNavManual   = new Color(0.90f, 0.40f, 0.05f, 0.40f);
    private static readonly Color kSceneNavAuto     = new Color(0.80f, 0.10f, 0.10f, 0.40f);
    private static readonly Color kSceneDungeonGrid = new Color(1.00f, 1.00f, 1.00f, 0.35f);

    public bool Enabled { get; set; } = true;

    // 現在 Prefab Mode で開いているプレハブが RoomPrefab と一致する場合のみ描画する
    // 一致しない状態で描画すると無関係な Scene に重なってしまうため
    public void Draw(SceneView sceneView, RoomGridData data)
    {
        if (data?.RoomPrefab == null) return;

        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage == null) return;
        if (stage.assetPath != AssetDatabase.GetAssetPath(data.RoomPrefab)) return;

        var bp = FieldBluePrintFinder.Find();
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

        float hs = navCellSize * 0.5f * kCellFillRatio;	// hs を navCellSize 未満にすることでセル境界線を自然に表現している
        for (int ny = 0; ny < navH; ny++)
        {
            for (int nx = 0; nx < navW; nx++)
            {
                bool autoB  = NavCellClassifier.IsAutoBlocked(data, nx, ny, sub);
                bool isDoor = !autoB && NavCellClassifier.IsDoorBorder(data, nx, ny, sub);
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
                    new Vector3(cx - hs, kCellY, cz - hs),
                    new Vector3(cx + hs, kCellY, cz - hs),
                    new Vector3(cx + hs, kCellY, cz + hs),
                    new Vector3(cx - hs, kCellY, cz + hs));
            }
        }

        Handles.color = kSceneDungeonGrid;
        for (int gx = 0; gx <= data.GridSize.x; gx++)
        {
            float lx = gx * dungeonCellSize - halfW;
            Handles.DrawAAPolyLine(kGridLineWidth,
                new Vector3(lx, kGridLineY, -halfD),
                new Vector3(lx, kGridLineY,  halfD));
        }
        for (int gz = 0; gz <= data.GridSize.y; gz++)
        {
            float lz = gz * dungeonCellSize - halfD;
            Handles.DrawAAPolyLine(kGridLineWidth,
                new Vector3(-halfW, kGridLineY, lz),
                new Vector3( halfW, kGridLineY, lz));
        }

        DetectionVolumeDrawer.Draw(halfW, halfD, yMin, yMax);
    }
}
