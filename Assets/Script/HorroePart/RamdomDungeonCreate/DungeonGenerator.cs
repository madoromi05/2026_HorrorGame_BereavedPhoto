/// <summary>
/// ダンジョン生成の全体フローを統括するクラス
/// 各PlacerとBuilderを組み立て順に呼び出す
/// </summary>
using DungeonSystem;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private FieldBluePrint m_bluePrint;
    [SerializeField] private RoomDataBase m_roomDataBase;
    [SerializeField] private CorridorDataBase m_corridorDataBase;

    // 部屋・通路をHierarchy上で分けて管理するための親オブジェクト
    [SerializeField] private Transform m_roomParent;
    [SerializeField] private Transform m_corridorParent;

    private DungeonGridBuilder m_gridBuilder;
    private SectionPlacer m_sectionPlacer;
    private CorridorPlacer m_corridorPlacer;

    private void Start()
    {
        Generate();
    }

    /// <summary>
    /// ダンジョンを生成する
    /// グリッド構築 → 部屋配置 → 通路配置の順に実行する
    /// </summary>
    public void Generate()
    {
        Initialize();

        var grid = m_gridBuilder.Build(m_bluePrint);
        m_sectionPlacer.Place(m_bluePrint.sections, m_roomParent);
        m_corridorPlacer.Place(grid, m_corridorParent);
    }

    private void Initialize()
    {
        m_gridBuilder = new DungeonGridBuilder();
        m_sectionPlacer = new SectionPlacer(m_roomDataBase, m_bluePrint.gridSize);
        m_corridorPlacer = new CorridorPlacer(m_corridorDataBase, m_bluePrint.gridSize);
    }
}