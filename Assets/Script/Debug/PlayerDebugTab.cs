using HorrorGame.Item;
using UnityEngine;

/// <summary>
/// デバッグ GUI の「プレイヤー」タブ。無敵・ゲームオーバー発生・移動速度の操作と、移動・ブレス・ダッシュ・所持品の状態を出す
/// GameDebugGUI が持ち、このタブが選ばれている間 OnGUI のたびに Draw を呼ぶ
/// プレイヤーの参照は探さない（DebugGUIContext から読むだけ）
/// </summary>
public class PlayerDebugTab : IDebugTab
{
    private const float kSectionSpace          = 6f;
    private const float kInvincibleButtonWidth = 90f;

    private static readonly (string Label, float Speed)[] kMoveSpeedPresets =	// 移動速度ボタンの表示と設定値。左から並ぶ
    {
        ("10", 10f),
        ("25", 25f),
        ("50", 50f),
    };

    public string Title => "プレイヤー";

    public void Draw(DebugGUIContext context, DebugGUIStyles gui)
    {
        var mover           = context.Mover;
        var gameOverHandler = context.GameOverHandler;

        // ---- デバッグ操作（無敵・速度変更） ----
        gui.SectionHeader("デバッグ");

        GUILayout.BeginHorizontal();
        gui.Label("無敵:", Color.white);
        gui.Label(GameOverHandler.DebugInvincible ? "ON" : "OFF",
                  GameOverHandler.DebugInvincible ? DebugGUIStyles.kDoneColor : Color.gray);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(GameOverHandler.DebugInvincible ? "無敵 OFF" : "無敵 ON", GUILayout.Width(kInvincibleButtonWidth)))
            GameOverHandler.DebugInvincible = !GameOverHandler.DebugInvincible;
        GUILayout.EndHorizontal();

        // ゲームオーバー演出（静止 → ホワイトノイズ → タイトル）を敵に捕まらずに確認するためのボタン
        // 敵を渡さないため「敵の方を向く」段は飛び、静止から先だけが再生される
        bool canTriggerGameOver = gameOverHandler != null && !GameOverHandler.DebugInvincible;
        GUI.enabled = canTriggerGameOver;
        if (GUILayout.Button("ゲームオーバー発生（演出確認）"))
            gameOverHandler.TriggerGameOver();
        GUI.enabled = true;

        // 押しても無反応に見える条件は理由を出す（無敵中は TriggerGameOver 自体が弾かれるため）
        if (GameOverHandler.DebugInvincible)
            gui.Label("※ 無敵 ON の間は発生しません", Color.gray);
        else if (gameOverHandler == null)
            gui.Label("※ このシーンに GameOverHandler がありません", Color.gray);

        if (mover != null)
        {
            gui.Label($"移動速度: {mover.MoveSpeed:F0}", Color.white);
            GUILayout.BeginHorizontal();
            foreach (var preset in kMoveSpeedPresets)
            {
                if (GUILayout.Button(preset.Label)) mover.SetMoveSpeed(preset.Speed);
            }
            GUILayout.EndHorizontal();
        }
        else gui.Label("PlayerMover: 未検出（速度変更不可）", Color.red);

        GUILayout.Space(kSectionSpace);
        gui.SectionHeader("移動状態");
        if (mover != null)
            gui.Label($"MoveState: {mover.CurrentMoveState}", Color.white);
        else
            gui.Label("PlayerMover: 未検出", Color.red);

        GUILayout.Space(kSectionSpace);
        gui.SectionHeader("ブレス");
        var breath = context.Breath;
        if (breath != null)
        {
            gui.Label($"息止め中: {breath.IsHoldingBreath}",  Color.white);
            gui.Label($"息切れ中: {breath.IsGasping}",
                      breath.IsGasping ? Color.red : Color.white);
        }
        else gui.Label("PlayerBreath: 未検出", Color.gray);

        GUILayout.Space(kSectionSpace);
        gui.SectionHeader("ダッシュ");
        var dash = context.Dash;
        if (dash != null)
        {
            gui.Label($"疲労中: {dash.IsExhausted}",
                      dash.IsExhausted ? Color.red : Color.white);
        }
        else gui.Label("PlayerDashController: 未検出", Color.gray);

        GUILayout.Space(kSectionSpace);
        gui.SectionHeader("インベントリ");
        var inventory = context.Inventory;
        if (inventory != null)
        {
            foreach (ItemType t in System.Enum.GetValues(typeof(ItemType)))
            {
                bool has = inventory.HasItem(t);
                gui.Label(has ? $"[✓] {t}" : $"[  ] {t}",
                          has ? DebugGUIStyles.kDoneColor : Color.gray);
            }
        }
        else gui.Label("Inventory: 未検出", Color.red);
    }
}
