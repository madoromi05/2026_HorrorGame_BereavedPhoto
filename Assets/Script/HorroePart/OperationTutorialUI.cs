using UnityEngine;

namespace HorrorGame.UI
{
    /// <summary>
    /// ゲーム開始時に画面へ表示しているチュートリアル用の操作説明UI（OperationCanvas）を制御するヘルパー。
    /// 本を初めて読んだ時やスタートルームを出た時に HideIfActive() で非表示にする。
    /// 対象UIはシーン直置きだが、参照する側（本・出口）はランタイム生成のため
    /// SerializeField 参照できない。そのため名前でシーンから取得する。
    /// </summary>
    public static class OperationTutorialUI
    {
        // シーン内の操作説明UIルートのGameObject名。
        private const string kCanvasName = "OperationCanvas";

        /// <summary>
        /// 操作説明UIが有効なら非表示にする。既に非表示、または存在しない場合は何もしない。
        /// </summary>
        public static void HideIfActive()
        {
            // GameObject.Find はアクティブなオブジェクトのみ対象。
            // 一度非表示にすると以降は null が返り、二重処理にならない。
            var canvas = GameObject.Find(kCanvasName);
            if (canvas != null && canvas.activeSelf)
                canvas.SetActive(false);
        }
    }
}
