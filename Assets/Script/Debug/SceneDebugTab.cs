using HorrorGame.Item;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// デバッグ GUI の「シーン」タブ。今のシーンとステージを出し、シーンの直接遷移・鍵の全取得・FPS 表示と TimeScale の切り替えを出す
/// GameDebugGUI が持ち、このタブが選ばれている間 OnGUI のたびに Draw を呼ぶ
/// ステージの進行は変えない（シーンを読み込むだけ）
/// </summary>
public class SceneDebugTab : IDebugTab
{
    private const float kSectionSpace = 6f;
    private const float kMinDeltaTime = 0.001f;	// FPS の割り算で 0 除算しないための下限

    private static readonly (string Label, float Scale)[] kTimeScalePresets =	// TimeScale ボタンの表示と設定値。左から並ぶ
    {
        ("x0.5", 0.5f),
        ("x1.0", 1f),
        ("x2.0", 2f),
        ("x5.0", 5f),
    };

    public string Title => "シーン";

    public void Draw(DebugGUIContext context, DebugGUIStyles gui)
    {
        var mgr = GameProgressManager.Instance;
        string curScene = SceneManager.GetActiveScene().name;
        string curStage = mgr != null ? mgr.CurrentStage.ToString() : "N/A";

        gui.SectionHeader($"シーン: {curScene}  |  ステージ: {curStage}");

        // ---- シーン直接遷移（ステージ変更なし） ----
        GUILayout.Space(kSectionSpace);
        gui.Label("シーン直接遷移（ステージ変更なし）:", Color.gray);

        GUILayout.BeginHorizontal();
        DrawSceneButton("TitleScene",   GameProgressManager.SceneTitleName);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        DrawSceneButton("HorrorScene", GameProgressManager.SceneHorrorName);
        DrawSceneButton("GameClear",   GameProgressManager.SceneGameClear);
        GUILayout.EndHorizontal();

        // ---- デバッグ操作 ----
        GUILayout.Space(kSectionSpace);
        gui.SectionHeader("デバッグ操作");

        if (GUILayout.Button("鍵 全取得"))
        {
            var inventory = context.Inventory;
            if (inventory != null)
            {
                foreach (ItemType t in System.Enum.GetValues(typeof(ItemType)))
                    inventory.DebugAddItem(t);
            }
            else DebugCustom.LogWarning("[Debug] Inventory not found");
        }

        // ---- FPS / TimeScale ----
        GUILayout.Space(kSectionSpace);
        gui.SectionHeader("FPS / TimeScale");
        gui.Label($"{(1f / Mathf.Max(Time.deltaTime, kMinDeltaTime)):F0} fps    TimeScale: {Time.timeScale:F2}", Color.white);
        GUILayout.BeginHorizontal();
        foreach (var preset in kTimeScalePresets)
        {
            if (GUILayout.Button(preset.Label)) Time.timeScale = preset.Scale;
        }
        GUILayout.EndHorizontal();
    }

    private static void DrawSceneButton(string label, string sceneName)
    {
        bool isCurrent = SceneManager.GetActiveScene().name == sceneName;
        var prev = GUI.enabled;
        GUI.enabled = !isCurrent;
        if (GUILayout.Button(isCurrent ? $"▶ {label} (現在)" : label))
            SceneManager.LoadScene(sceneName);
        GUI.enabled = prev;
    }
}
