namespace HorrorGame.Interaction
{
    /// <summary>
    /// インタラクトのヒント文字列を組み立てる係
    /// キーの表記をここだけに置き、ドア・メモ・アイテムで同じ見た目にする
    /// </summary>
    public static class InteractHint
    {
        private const string kKeyLabel = "【E】";	// InputSystem_Actions の Interact（キーボードの E）

        public static string Format(string action) => kKeyLabel + action;
    }
}
