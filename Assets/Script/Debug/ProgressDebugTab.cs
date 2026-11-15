using UnityEngine;

/// <summary>
/// デバッグ GUI の「進行」タブ。敵の種類ごとの解析率を出し、種類ごとに解析を完了させるボタンを出す
/// GameDebugGUI が持ち、このタブが選ばれている間 OnGUI のたびに Draw を呼ぶ
/// 解析率の計算や完了の判定はしない（EnemyAnalyzer に聞くだけ）
/// </summary>
public class ProgressDebugTab : IDebugTab
{
    private const float kSummarySpace        = 4f;	// 全体の完了表示と種類ごとの行の間
    private const float kCompleteButtonWidth = 60f;

    public string Title => "進行";

    public void Draw(DebugGUIContext context, DebugGUIStyles gui)
    {
        gui.SectionHeader("解析状況");
        DrawAnalyzerSection(context.Analyzer, gui);
    }

    private static void DrawAnalyzerSection(EnemyAnalyzer analyzer, DebugGUIStyles gui)
    {
        if (analyzer == null)
        {
            gui.Label("EnemyAnalyzer: 未検出", Color.gray);
            return;
        }

        bool allDone = analyzer.IsComplete;	// 全体の完了判定（全種類が100%か）
        gui.Label(allDone ? "全種類 ✓ 完了" : "解析中（未完了の種類あり）",
                  allDone ? DebugGUIStyles.kDoneColor : Color.white);

        GUILayout.Space(kSummarySpace);

        foreach (EnemyType type in System.Enum.GetValues(typeof(EnemyType)))	// 種類ごとに解析率を表示し、その種類だけを完了させる
        {
            float pct  = analyzer.GetAnalyzePercent(type);
            bool  done = pct >= EnemyAnalyzer.kCompletePercent;

            GUILayout.BeginHorizontal();
            gui.Label($"{type}: {pct:F0}%", done ? DebugGUIStyles.kDoneColor : Color.white);
            GUILayout.FlexibleSpace();
            var prev = GUI.enabled;
            GUI.enabled = !done;
            if (GUILayout.Button("完了", GUILayout.Width(kCompleteButtonWidth)))
                analyzer.DebugCompleteType(type);
            GUI.enabled = prev;
            GUILayout.EndHorizontal();
        }
    }
}
