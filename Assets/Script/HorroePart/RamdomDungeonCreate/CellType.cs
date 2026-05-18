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

    public enum Direction
    {
        North,
        East,
        South,
        West,
    }

    //部屋の種類
    public enum RoomType
    {
        Normal,
        Start,
        Mother,
        Father,
        Sister,
    }
}