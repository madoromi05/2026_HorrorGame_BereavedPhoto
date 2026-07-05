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
        var sectionPlacer  = new SectionPlacer(_roomDataBase, _bluePrint.OneGridSize, _playerTransform, _playerSpawnOffsetY);
        var corridorPlacer = new CorridorPlacer(_corridorDataBase, _bluePrint.OneGridSize);

        var (grid, sections) = gridBuilder.Build(_bluePrint, _roomDataBase);

        // 部屋・廊下を配置してから NavMesh をベイクし、その後に敵を生成する。
        // 敵の NavMeshAgent は有効な NavMesh が存在しないと配置に失敗するため順序が重要。
        var playerTransform = sectionPlacer.Place(sections, _roomParent);

        // 部屋プレハブに NavMeshAgent が埋め込まれている場合、Instantiate 時点では
        // NavMesh が未ベイクのためエラーになる。ベイク完了まで一時的に無効化する。
        var roomEmbeddedAgents = _roomParent.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>();

        corridorPlacer.Place(grid, _corridorParent);
        corridorPlacer.Combine(_corridorParent);
        _navMeshSurface?.BuildNavMesh();

        // ベイク後に再有効化し、Warp で NavMesh 上の正しい位置に配置する。
        foreach (var agent in roomEmbeddedAgents)
        {
            agent.enabled = true;
            agent.Warp(agent.transform.position);
        }

        enemySpawner.Place(sections, _enemyParent);

        OnRoomPlaced?.Invoke(_roomParent);

        if (_isDebugMode && _debugParent != null)
        {
            var debugVisualizer = new DungeonDebugVisualizer(_bluePrint.OneGridSize);
            debugVisualizer.Visualize(grid, sections, _debugParent);
        }
    }
}