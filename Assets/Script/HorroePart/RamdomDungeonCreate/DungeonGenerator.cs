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
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private float _playerSpawnOffsetY = 0f;
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private float _enemySpawnOffsetY = 0f;

    [Header("Debug")]
    [SerializeField] private bool _isDebugMode;
    [SerializeField] private Transform _debugParent;

    private DungeonGridBuilder _gridBuilder;
    private DungeonDebugVisualizer _debugVisualizer;
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
        Initialize();

        var (grid, sections) = _gridBuilder.Build(_bluePrint, _roomDataBase);

        _sectionPlacer.Place(sections, _roomParent);
        _corridorPlacer.Place(grid, _corridorParent);

        if (_isDebugMode && _debugParent != null)
            _debugVisualizer.Visualize(grid, sections, _debugParent);
    }

    private void Initialize()
    {
        _gridBuilder = new DungeonGridBuilder();
        _sectionPlacer = new SectionPlacer(_roomDataBase, _bluePrint.OneGridSize, _playerPrefab, _playerSpawnOffsetY, _enemyPrefab, _enemySpawnOffsetY);
        _corridorPlacer = new CorridorPlacer(_corridorDataBase, _bluePrint.OneGridSize);
        _debugVisualizer = new DungeonDebugVisualizer(_bluePrint.OneGridSize);
    }
}