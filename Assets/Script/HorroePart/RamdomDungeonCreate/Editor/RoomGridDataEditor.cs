using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// RoomGridData の Inspector カスタムエディタの窓口（Facade）
/// ルームグリッドのセル種別編集、敵ナビゲーションセルの手動設定、
/// および Auto-fill（プレハブコライダーからの自動生成）を提供する
/// Prefab Mode 中は Scene View に検知ボックスとナビセルを Gizmo 表示する
/// ここが決めるのは Inspector の並びと Scene View への登録だけで、各セクションは
/// RoomGridPainter・NavGridSectionDrawer・NavScenePreview へ渡す
/// 折りたたみの開閉と係のインスタンスはこのエディタが持ち、再描画をまたいで残す
/// </summary>
[CustomEditor(typeof(RoomGridData))]
public class RoomGridDataEditor : Editor
{
    private bool _showNavGrid = true;

    private RoomGridPainter      _roomGrid;
    private NavGridSectionDrawer _navSection;
    private NavScenePreview      _scenePreview;

    // SceneView のデリゲートはエディタが非選択になっても登録が残るため、
    // OnDisable で確実に解除しないと他のオブジェクト選択時にも描画が走り続ける
    private void OnEnable()
    {
        _roomGrid     = new RoomGridPainter(Repaint);
        _scenePreview = new NavScenePreview();
        _navSection   = new NavGridSectionDrawer(Repaint, _scenePreview);
        SceneView.duringSceneGui += DrawSceneGUI;
    }
    private void OnDisable() => SceneView.duringSceneGui -= DrawSceneGUI;

    public override void OnInspectorGUI()
    {
        var data = (RoomGridData)target;

        _roomGrid.DrawSizeField(data);

        if (data.GridSize.x <= 0 || data.GridSize.y <= 0) return;

        if (data.DoorPositions   == null) data.DoorPositions   = new List<Vector2Int>();
        if (data.WallPositions   == null) data.WallPositions   = new List<Vector2Int>();
        if (data.PlayerPositions == null) data.PlayerPositions = new List<Vector2Int>();

        EditorGUILayout.Space(6);

        // ---- Room Grid ----
        _roomGrid.DrawSection(data);

        // ---- Enemy Navigation Grid ----
        EditorGUILayout.Space(10);
        _showNavGrid = EditorGUILayout.Foldout(_showNavGrid, "Enemy Navigation Grid", true, EditorStyles.foldoutHeader);
        if (_showNavGrid)
            _navSection.Draw(data);
    }

    private void DrawSceneGUI(SceneView sceneView)
    {
        if (!_scenePreview.Enabled) return;
        _scenePreview.Draw(sceneView, (RoomGridData)target);
    }
}
