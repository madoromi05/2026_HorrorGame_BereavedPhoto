using System.Collections.Generic;
using DungeonSystem;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NavMeshAgent を使って通路ウェイポイントをマップ全体に渡って巡回するクラス。
/// 遠距離優先の重み付き抽選で選んだウェイポイントへ SetDestination で移動する。
/// ウェイポイント到着後に次を選ぶことでマップ全体へ分散する。
/// </summary>
public class MapWanderer : MonoBehaviour, IEnemyBehavior
{
    [SerializeField] private float _wanderSpeed   = 4f;
    [SerializeField] private float _arrivalRadius = 1.5f;

    private NavMeshAgent _agent;
    private List<Vector3> _corridorWaypoints = new List<Vector3>();
    private Vector3 _currentTarget;
    private bool    _isInitialized;

    private Dictionary<Vector3, float> _waypointCooldowns = new Dictionary<Vector3, float>();
    private readonly List<Vector3>     _cooldownKeyBuffer = new List<Vector3>();
    private const float kWaypointCooldown = 15f;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (!_isInitialized || _waypointCooldowns.Count == 0) return;

        // クールダウンをカウントダウン（Tick が呼ばれない追跡中も進める）
        _cooldownKeyBuffer.Clear();
        _cooldownKeyBuffer.AddRange(_waypointCooldowns.Keys);
        foreach (var key in _cooldownKeyBuffer)
        {
            _waypointCooldowns[key] -= Time.deltaTime;
            if (_waypointCooldowns[key] <= 0f)
                _waypointCooldowns.Remove(key);
        }
    }

    public void SetGrid(GridType[,] grid, float gridSize)
    {
        _corridorWaypoints.Clear();
        _waypointCooldowns.Clear();

        for (int x = 0; x < grid.GetLength(0); x++)
        for (int y = 0; y < grid.GetLength(1); y++)
        {
            var cell = grid[x, y];
            if (cell != GridType.Corridor && cell != GridType.Door) continue;
            _corridorWaypoints.Add(new Vector3((x + 0.5f) * gridSize, 0f, (y + 0.5f) * gridSize));
        }

        if (_corridorWaypoints.Count == 0)
        {
            DebugCustom.LogWarning("[MapWanderer] 通路セルが見つかりません。徘徊を停止します。");
            return;
        }

        _currentTarget = PickWeightedWaypoint();
        _agent.SetDestination(_currentTarget);
        _isInitialized = true;
    }

    public void OnChaseEnded()
    {
        if (!_isInitialized) return;
        _currentTarget = PickWeightedWaypoint();
        _agent.SetDestination(_currentTarget);
    }

    public void Tick()
    {
        if (!_isInitialized || _agent == null) return;

        _agent.speed = _wanderSpeed;

        if (!_agent.pathPending && _agent.remainingDistance < _arrivalRadius)
        {
            _currentTarget = PickWeightedWaypoint();
            _agent.SetDestination(_currentTarget);
        }
    }

    // ---------------- ウェイポイント抽選 ----------------

    /// <summary>
    /// 遠距離優先の重み付き抽選。
    /// 1. クールダウン中でない・直前ターゲット以外から選ぶ
    /// 2. 全候補がクールダウン中ならクールダウン無視でフォールバック
    /// </summary>
    private Vector3 PickWeightedWaypoint()
    {
        var candidates = new List<Vector3>();
        foreach (var wp in _corridorWaypoints)
        {
            if (wp == _currentTarget) continue;
            if (_waypointCooldowns.ContainsKey(wp)) continue;
            candidates.Add(wp);
        }

        if (candidates.Count == 0)
        {
            foreach (var wp in _corridorWaypoints)
                if (wp != _currentTarget) candidates.Add(wp);
        }

        if (candidates.Count == 0)
            return _corridorWaypoints[Random.Range(0, _corridorWaypoints.Count)];

        var selected = WeightedSelectByDistance(candidates);
        _waypointCooldowns[selected] = kWaypointCooldown;
        return selected;
    }

    private Vector3 WeightedSelectByDistance(List<Vector3> candidates)
    {
        float totalWeight = 0f;
        foreach (var wp in candidates)
            totalWeight += (wp - transform.position).magnitude;

        float threshold  = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (var wp in candidates)
        {
            cumulative += (wp - transform.position).magnitude;
            if (cumulative >= threshold) return wp;
        }
        return candidates[candidates.Count - 1];
    }
}
