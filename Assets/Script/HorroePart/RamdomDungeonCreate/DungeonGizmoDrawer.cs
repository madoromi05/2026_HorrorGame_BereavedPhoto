using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DungeonDebugVisualizer から登録されたボックスを Gizmos で描画する。
/// シェーダー・マテリアルを使わないため色が確実に表示される。
/// Scene ビューの Gizmos ボタンが ON になっていれば常に表示。
/// </summary>
public class DungeonGizmoDrawer : MonoBehaviour
{
    private readonly List<(Vector3 center, Vector3 size, Color color)> _boxes = new();

    public void AddBox(Vector3 center, Vector3 size, Color color)
        => _boxes.Add((center, size, color));

    private void OnDrawGizmos()
    {
        foreach (var (center, size, color) in _boxes)
        {
            Gizmos.color = color;
            Gizmos.DrawCube(center, size);
        }
    }
}
