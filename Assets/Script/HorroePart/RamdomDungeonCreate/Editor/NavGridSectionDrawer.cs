using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector の「Enemy Navigation Grid」セクションの並びを決める係
/// Subdivision → 検知ボックス設定 → ナビグリッド → セル数とリセット → Auto-fill の順に描く
/// RoomGridDataEditor が持ち、折りたたみが開いているときに OnInspectorGUI から呼ぶ
/// 検知ボックス設定は HeightCheckSectionDrawer、グリッドは NavGridDrawer、焼き込みは NavGridBaker に任せ、ここでは描かない
/// </summary>
internal class NavGridSectionDrawer
{
    private const int kMinSubdivision = 1;	// Nav Subdivision スライダーの下限
    private const int kMaxSubdivision = 10;	// Nav Subdivision スライダーの上限

    private readonly System.Action            _repaint;	// 持ち主の Inspector を再描画する
    private readonly HeightCheckSectionDrawer _heightCheck;
    private readonly NavGridDrawer            _navGrid;

    public NavGridSectionDrawer(System.Action repaint, NavScenePreview scenePreview)
    {
        _repaint     = repaint;
        _heightCheck = new HeightCheckSectionDrawer(repaint, scenePreview);
        _navGrid     = new NavGridDrawer(repaint);
    }

    public void Draw(RoomGridData data)
    {
        DrawSubdivision(data);

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
        _heightCheck.Draw(data);

        DrawNavGrid(data, sub, navW, navH);
        DrawAutoFill(data);
    }

    private static void DrawSubdivision(RoomGridData data)
    {
        EditorGUI.BeginChangeCheck();
        var newSub = EditorGUILayout.IntSlider("Nav Subdivision", data.EnemyNavSubdivision, kMinSubdivision, kMaxSubdivision);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(data, "Change Nav Subdivision");
            data.EnemyNavSubdivision = newSub;
            data.EnemyNavWallCells.Clear();
            EditorUtility.SetDirty(data);
        }
    }

    private void DrawNavGrid(RoomGridData data, int sub, int navW, int navH)
    {
        EditorGUILayout.Space(6);
        _navGrid.DrawLegend();
        EditorGUILayout.Space(3);
        _navGrid.DrawGrid(data, navW, navH);
        EditorGUILayout.Space(4);

        int autoB    = NavCellClassifier.CountAutoBlocked(data, sub);
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
    }

    private void DrawAutoFill(RoomGridData data)
    {
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

        var blueprint = FieldBluePrintFinder.Find();
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
                NavGridBaker.Bake(data, blueprint.OneGridSize);
                _repaint();
            }
        }
    }
}
