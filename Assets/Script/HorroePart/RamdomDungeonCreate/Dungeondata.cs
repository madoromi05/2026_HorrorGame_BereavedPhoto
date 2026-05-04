using System.Collections.Generic;

namespace DungeonSystem
{
    /// <summary>
    /// ダンジョン全体のデータ保持クラス。
    /// ロジックを持たず、データの入れ物に徹する。
    /// DungeonDirectorが各クラスに渡して共有する。
    /// </summary>
    public class DungeonData
    {
        public List<RoomData> Rooms { get; } = new List<RoomData>();
        public List<CorridorData> Corridors { get; } = new List<CorridorData>();
    }
}