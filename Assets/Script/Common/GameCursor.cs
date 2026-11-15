using UnityEngine;

/// <summary>
/// マウスカーソルの固定と表示を切り替える係
/// 固定と非表示は必ず組で変えるので、片方だけ書き換える漏れを防ぐ
/// </summary>
public static class GameCursor
{
    public static bool IsLocked => Cursor.lockState == CursorLockMode.Locked && !Cursor.visible;

    public static void Lock()	// 本編の操作中。中心に固定して隠す
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    public static void Unlock()	// メニューや画面のボタンを押すとき。自由に動かして見せる
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }
}
