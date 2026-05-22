namespace HorrorGame.Item
{
    /// <summary>
    /// ゲーム内で取得可能なアイテムの種別。
    /// Inventoryはこの型をキーに所持フラグを管理する。
    /// 新アイテムを追加する際はここに列挙子を追加し、
    /// 対応するItemDataアセットを作成すること。
    /// </summary>
    public enum ItemType
    {
        // --- 鍵・解錠系 ---
        KeyFather,
        KeyMother,

        // --- メモ・ドキュメント系 ---
        NoteEntrance,
        NoteBasement,
        DocumentResearch,
    }
}