/// <summary>
/// ダンジョン生成の起点となるMonoBehaviour。
/// GridBuilder・SectionPlacer・CorridorPlacerを順に呼び出し、
/// グリッド構築 → 部屋配置 → 通路配置の流れを制御する。
/// NavMesh はダンジョン全体を配置し終えた後にランタイムでベイクする。
/// </summary>
using DungeonSystem;
using HorrorGame.UI;
using System;
using Unity.AI.Navigation;
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

    [Header("NavMesh")]
    [Tooltip("この GameObject にアタッチした NavMeshSurface を指定する。" +
             "部屋・廊下を全て配置した後にランタイムでベイクし単一の NavMesh を生成する。")]
    [SerializeField] private NavMeshSurface _navMeshSurface;

    [Header("Debug")]
    [SerializeField] private bool _isDebugMode;
    [SerializeField] private Transform _debugParent;

    // Plasyerの位置をItemInitializerが知る必要があるため必要
    public event Action<Transform> OnRoomPlaced;

    private SectionPlacer _sectionPlacer;
    private CorridorPlacer _corridorPlacer;

    /// <summary>
    /// Awake タイミングで外部から RoomDataBase を上書きする。
    /// HorrorSceneConfigurator が Generate() より前に呼び出す。
    /// </summary>
    public void SetRoomDataBase(RoomDataBase data) => _roomDataBase = data;

    private void Start()
    {
        DebugCustom.ValidateFields(this,
            (nameof(_bluePrint), _bluePrint),
            (nameof(_roomDataBase), _roomDataBase),
            (nameof(_corridorDataBase), _corridorDataBase),
            (nameof(_roomParent), _roomParent),
            (nameof(_corridorParent), _corridorParent),
            (nameof(_enemyParent), _enemyParent),
            (nameof(_playerTransform), _playerTransform));

        Generate();
    }

    /// <summary>
    /// ダンジョンを生成する。
    /// 複数箇所から呼び直せるよう、初期化も内包している。
    /// </summary>
    public void Generate()
    {
        var gridBuilder    = new DungeonGridBuilder();
        var enemySpawner   = new EnemySpawner(_roomDataBase, _bluePrint.OneGridSize, _enemySpawnOffsetY);
        var sectionPlacer  = new SectionPlacer(_roomDataBase, _bluePrint.OneGridSize, _playerTransform, _playerSpawnOffsetY, enemySpawner);
        var corridorPlacer = new CorridorPlacer(_corridorDataBase, _bluePrint.OneGridSize);

        var (grid, sections) = gridBuilder.Build(_bluePrint, _roomDataBase);

        sectionPlacer.Place(sections, _roomParent, _enemyParent, grid);
        corridorPlacer.Place(grid, _corridorParent);

        // 部屋・廊下を全て配置した後に NavMesh をランタイムベイクする。
        // 単一サーフェスでダンジョン全体を覆うため、プレハブ間の隙間は生じない。
        _navMeshSurface?.BuildNavMesh();

        OnRoomPlaced?.Invoke(_roomParent);

        if (_isDebugMode && _debugParent != null)
        {
            var debugVisualizer = new DungeonDebugVisualizer(_bluePrint.OneGridSize);
            debugVisualizer.Visualize(grid, sections, _debugParent);
        }
    }
}