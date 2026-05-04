namespace DungeonSystem
{
    /// <summary>
    /// 部屋の種別を定義する。
    /// 配置優先順: Start → Boss → Treasure → Rest → Elite → Normal
    /// </summary>
    public enum RoomType
    {
        Start,
        Normal,
        Rest,
    }
}