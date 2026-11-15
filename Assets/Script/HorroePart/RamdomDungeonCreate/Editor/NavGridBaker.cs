using UnityEditor;
using UnityEngine;

/// <summary>
/// 部屋プレハブのコライダーから EnemyNavWallCells を作り直す係（Auto-fill の中身）
/// NavGridSectionDrawer の Auto-fill ボタンから呼ぶ。Inspector の描画はしない
/// </summary>
internal static class NavGridBaker
{
    /// <summary>
    /// プレハブのコライダー情報を走査して EnemyNavWallCells を自動生成する
    /// プレハブのローカル原点は部屋中心に置かれている（SectionPlacer の配置規約）ため、
    /// ナビセルのグリッド座標を中心基準のローカル座標に変換してから Bounds 判定を行う
    /// isTrigger コライダーはナビゲーション障害物ではないのでスキップする
    /// </summary>
    public static void Bake(RoomGridData data, float dungeonCellSize)
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
                    if (NavCellClassifier.IsAutoBlocked(data, nx, ny, sub)) continue;

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
