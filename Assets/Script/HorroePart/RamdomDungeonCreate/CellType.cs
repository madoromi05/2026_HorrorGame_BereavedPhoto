namespace DungeonSystem
{
    //部屋、何もない場所のグリッドタイプ
    public enum GridType
    {
        Empty,
        Wall,
        Floor,
        Door,
        Corridor,
        PlayerPosition,
    }

    //通路のグリッドタイプ
    public enum CorridorType
    {
        Straight,
        Corner,
        T_Junction,
        Crossroad,
        DeadEnd,
    }

    //部屋の種類
    public enum RoomType
    {
        Start,
        Mother,
        Father,
        Sister,
        Normal,
    }
}