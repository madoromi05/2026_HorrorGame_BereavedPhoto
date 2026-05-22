/// <summary>
/// ダンジョン生成の起点となるMonoBehaviour。
/// GridBuilder・SectionPlacer・CorridorPlacerを順に呼び出し、
/// グリッド構築 → 部屋配置 → 通路配置の流れを制御する。
/// </summary>
using DungeonSystem;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private FieldBluePrint _bluePrint;
    [SerializeField] private RoomDataBase _roomDataBase;
    [SerializeField] private CorridorDataBase _corridorDataBase;

    [SerializeField] private Transform _roomParent;
    [SerializeField] private Transform _corridorParent;
    [SerializeField] private Transform _enemyParent;
    [SerializeField] private Transform _playerTransform;

    [SerializeField] private float _playerSpawnOffsetY = 0f;
    [SerializeField] private float _enemySpawnOffsetY = 0f;

    [Header("Debug")]
    [SerializeField] private bool _isDebugMode;
    [SerializeField] private Transform _debugParent;
    [SerializeField] private bool _isEnemyLookDebug;

    private SectionPlacer _sectionPlacer;
    private CorridorPlacer _corridorPlacer;

    private void Start()
    {
        Generate();
    }

    /// <summary>
    /// ダンジョンを生成する。
    /// 複数箇所から呼び直せるよう、初期化も内包している。
    /// </summary>
    public void Generate()
    {
        var gridBuilder = new DungeonGridBuilder();
        var enemySpawner = new EnemySpawner(_roomDataBase, _bluePrint.OneGridSize, _enemySpawnOffsetY);
        var sectionPlacer = new SectionPlacer(_roomDataBase, _bluePrint.OneGridSize, _playerTransform, _playerSpawnOffsetY, enemySpawner);
        var corridorPlacer = new CorridorPlacer(_corridorDataBase, _bluePrint.OneGridSize);

        var (grid, sections) = gridBuilder.Build(_bluePrint, _roomDataBase);

        sectionPlacer.Place(sections, _roomParent, _enemyParent, grid, _isDebugMode && _isEnemyLookDebug);
        corridorPlacer.Place(grid, _corridorParent);

        if (_isDebugMode && _debugParent != null)
        {
            var debugVisualizer = new DungeonDebugVisualizer(_bluePrint.OneGridSize);
            debugVisualizer.Visualize(grid, sections, _debugParent);
        }
    }
}