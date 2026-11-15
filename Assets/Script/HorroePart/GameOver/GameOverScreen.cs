using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ゲームオーバーのときの画面の UI を扱う係
/// 今ある UI をすべて消すことと、最前面の黒い覆いを作ることだけをする。時間は持たない
/// </summary>
public static class GameOverScreen
{
    /// <summary>
    /// シーン内のルート Canvas をすべて無効にする。UI は単一の親にまとまっていないため
    /// ジャンプスケア（3D）と VHS ノイズ（描画エフェクト）は Canvas ではないため残る
    /// </summary>
    public static void HideAllUI()
    {
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (canvas.isRootCanvas) canvas.enabled = false;
    }

    /// <summary>
    /// 全画面の黒い覆いを、最前面の Canvas として作る。透明から始まる
    /// HideAllUI の後に作るので、無効化の影響を受けずに最前面へ出る。シーン遷移で自動破棄されるため後始末は不要
    /// </summary>
    public static CanvasGroup CreateBlackout()
    {
        var go = new GameObject("GameOverDarkenOverlay");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;	// 他の全 UI より手前に描画する

        var group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;						// 透明から開始
        group.blocksRaycasts = true;			// フェード中の入力を遮断する

        var image = go.AddComponent<Image>();
        image.color = Color.black;				// 不透明な黒（全画面）
        return group;
    }
}
