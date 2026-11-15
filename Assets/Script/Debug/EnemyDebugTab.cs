using UnityEngine;

/// <summary>
/// デバッグ GUI の「敵」タブ。登録されている敵を 1 体ずつ、追跡中か待機中かと種類ごとの解析率を出す
/// GameDebugGUI が持ち、このタブが選ばれている間 OnGUI のたびに Draw を呼ぶ
/// 敵を探したり操作したりはしない（EnemyRegistry.All を読むだけ）
/// </summary>
public class EnemyDebugTab : IDebugTab
{
    private const float kEnemyRowSpace = 2f;	// 敵 1 体ぶんの箱どうしの間

    public string Title => "敵";

    public void Draw(DebugGUIContext context, DebugGUIStyles gui)
    {
        var enemies = EnemyRegistry.All;
        gui.SectionHeader($"敵 ({enemies.Count}体)");

        if (enemies.Count == 0)
        {
            gui.Label("敵オブジェクトなし", Color.gray);
            return;
        }

        var analyzer = context.Analyzer;
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];

            var ghost = e.GetComponent<GhostIdentity>();
            string name = ghost != null ? $"[{ghost.GhostType}]" : $"[Enemy{i}]";
            bool activated = e.IsActivated;
            Color stateColor = activated ? Color.red : Color.white;

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            gui.Label($"{name}  {(activated ? "追跡中" : "待機中")}", stateColor);
            GUILayout.FlexibleSpace();
            if (analyzer != null && ghost != null)
            {
                float ap = analyzer.GetAnalyzePercent(ghost.GhostType);	// 解析率は種類（GhostType）ごとに共有される
                gui.Label($"解析: {ap:F0}%", ap >= EnemyAnalyzer.kCompletePercent ? DebugGUIStyles.kDoneColor : Color.white);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(kEnemyRowSpace);
        }
    }
}
