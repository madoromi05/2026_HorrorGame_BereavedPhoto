using System.Collections.Generic;
using DungeonSystem;
using UnityEngine;


/// <summary>
/// 通路セル（GridType.Corridor / Door）のワールド座標をウェイポイントとして順に移動する徘徊クラス。
/// EnemySpawner から SetGrid で通路座標リストとグリッドを受け取り、A* 経路探索で壁を迂回しながら
/// 重み付き抽選で選んだウェイポイントへ移動する。
///
/// マップ全体を巡回しやすくするため以下の3つの仕組みを持つ。
/// 1. 直前のターゲットを除外して連続同一選択を防ぐ。
/// 2. 遠いウェイポイントほど選ばれやすい距離重み付き抽選で偏りを抑える。
/// 3. 一度選んだウェイポイントに kWaypointCooldown 秒のクールダウンを設けて短期間の行き来を防ぐ。
///
/// 移動は DungeonPathfinder で生成した中継ウェイポイント列をノード単位で追従するため、
/// 入り組んだ通路でも壁詰まりが発生しない。
/// </summary>
public class MapWanderer : MonoBehaviour, IEnemyBehavior
{
    [SerializeField] private float _wanderSpeed = 4f;           // 徘徊中移動速度
    [SerializeField] private float _rotateSpeed = 10f;          // 目標方向への回転速度
    [SerializeField] private float _accelerationForce = 20f;    // 加速度
    [SerializeField] private float _arrivalRadius = 1.5f;       // 到着判定半径

    private Rigidbody _rb;
    private WallSlider _wallSlider;
    private DungeonPathfinder _pathfinder;

    private List<Vector3> _corridorWaypoints = new List<Vector3>();
    private Vector3 _currentTarget;
    private bool _isInitialized;

    // A* で求めた中継ウェイポイント列と現在追従中のインデックス
    private List<Vector3> _currentPath  = new List<Vector3>();
    private int _pathNodeIndex;

    private int _stuckCount;
    private float _stuckDecayTimer;
    private const float kStuckDecayDuration = 5f;
    private const int kTeleportStuckThreshold = 3;

    // ウェイポイントごとの残りクールダウン時間。0以下なら選出可能
    private Dictionary<Vector3, float> _waypointCooldowns = new Dictionary<Vector3, float>();

    // クールダウン更新時のキー列挙バッファ（毎フレームの List 確保を避けて使い回す）
    private readonly List<Vector3> _cooldownKeyBuffer = new List<Vector3>();

    // 一度選んだウェイポイントを再選禁止にする期間
    private const float kWaypointCooldown = 15f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _wallSlider = GetComponent<WallSlider>();
    }

    private void FixedUpdate()
    {
        if (!_isInitialized) return;

        if (_stuckDecayTimer > 0f)
        {
            _stuckDecayTimer -= Time.fixedDeltaTime;
            if (_stuckDecayTimer <= 0f)
                _stuckCount = 0;
        }

        // クールダウンをカウントダウンする
        if (_waypointCooldowns.Count > 0)
        {
            _cooldownKeyBuffer.Clear();
            _cooldownKeyBuffer.AddRange(_waypointCooldowns.Keys);
            foreach (var key in _cooldownKeyBuffer)
            {
                _waypointCooldowns[key] -= Time.fixedDeltaTime;
                if (_waypointCooldowns[key] <= 0f)
                    _waypointCooldowns.Remove(key);
            }
        }
    }

    /// <summary>
    /// グリッドとEnemySpawnerが構築した共有パスファインダーを受け取り初期化する。
    /// Corridor・Door セルのウェイポイント座標リストを構築し、最初のパスを計算する。
    /// EnemySpawner が Instantiate 後に呼び出すこと。
    /// </summary>
    public void SetGrid(GridType[,] grid, float gridSize, DungeonPathfinder pathfinder)
    {
        _pathfinder = pathfinder;
        _corridorWaypoints.Clear();
        _waypointCooldowns.Clear();

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                var cell = grid[x, y];
                if (cell != GridType.Corridor && cell != GridType.Door) continue;

                _corridorWaypoints.Add(new Vector3(
                    (x + 0.5f) * gridSize,
                    0f,
                    (y + 0.5f) * gridSize
                ));
            }
        }

        if (_corridorWaypoints.Count == 0)
        {
            DebugCustom.LogWarning("[MapWanderer] 通路セルが見つかりませんでした。徘徊を停止します。");
            return;
        }

        _currentTarget = PickWeightedWaypoint();
        ComputePath(_currentTarget);
        _isInitialized = true;
    }

    /// <summary>
    /// 追跡終了後は追跡中に移動した位置から新しいウェイポイントへ向かうパスを再計算する。
    /// </summary>
    public void OnChaseEnded()
    {
        if (!_isInitialized) return;
        _currentTarget = PickWeightedWaypoint();
        ComputePath(_currentTarget);
    }

    public void Tick()
    {
        if (!_isInitialized) return;

        // 角詰まり検出時は壁から離れる方向のウェイポイントへパスを再計算する
        if (_wallSlider != null && _wallSlider.ConsumeStuck(out var escapeNormal))
        {
            _currentTarget = PickEscapeTarget(escapeNormal);
            ComputePath(_currentTarget);

            _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);

            _stuckDecayTimer = kStuckDecayDuration;
            _stuckCount++;

            if (_stuckCount >= kTeleportStuckThreshold)
            {
                _stuckCount = 0;
                _wallSlider.TryTeleportEscape(_rb, ~(1 << gameObject.layer));
            }
            else
            {
                _rb.AddForce(escapeNormal * _accelerationForce * 2f, ForceMode.Impulse);
            }
        }

        // 現在のパスノードに到達したか、あるいはスキップできるか判定して次へ進める
        while (_pathNodeIndex < _currentPath.Count)
        {
            var node = _currentPath[_pathNodeIndex];
            var dx = node.x - transform.position.x;
            var dz = node.z - transform.position.z;

            // 距離が到着判定以内ならクリア
            if (dx * dx + dz * dz <= _arrivalRadius * _arrivalRadius)
            {
                _pathNodeIndex++;
                continue;
            }

            // 角に配置されたノードへ無理に近づいて引っかかるのを防ぐため、
            // 「さらに次のノード」へ直接視線が通るなら、現在のノードをスキップする。
            if (_pathNodeIndex < _currentPath.Count - 1)
            {
                var nextNode = _currentPath[_pathNodeIndex + 1];
                var origin = transform.position + Vector3.up * 1f;
                var target = nextNode + Vector3.up * 1f;
                var dir = target - origin;

                // 自身（敵）のレイヤーを無視して壁との間をレイキャスト
                int layerMask = ~(1 << gameObject.layer);

                // 次のノードへの直線上に障害物がなければ、現在のノードをスキップ
                if (!Physics.Raycast(origin, dir.normalized, dir.magnitude, layerMask))
                {
                    _pathNodeIndex++;
                    continue;
                }
            }

            break;
        }

        // パスを全て辿り終えたら次のウェイポイントを選んでパスを再計算する
        if (_pathNodeIndex >= _currentPath.Count)
        {
            _currentTarget = PickWeightedWaypoint();
            ComputePath(_currentTarget);
        }

        // 現在のパスノードへ向かって移動する
        if (_pathNodeIndex < _currentPath.Count)
            MoveToward(_currentPath[_pathNodeIndex]);
    }

    private void ComputePath(Vector3 target)
    {
        _currentPath   = _pathfinder != null
            ? _pathfinder.FindPath(transform.position, target, transform.position.y)
            : new List<Vector3> { target };
        _pathNodeIndex = 0;
    }

    /// <summary>
    /// マップ全体を巡回しやすくするための重み付き抽選。
    /// 以下の優先順位で候補を絞り込み、距離に比例した重みでランダム選出する。
    /// 1. クールダウン中でない、かつ直前のターゲットでないウェイポイント
    /// 2. クールダウン中のみ（全候補がクールダウン中の場合のフォールバック）
    /// 距離が遠いほど重みが大きくなるため、近場の往来ではなくマップ全体への移動が促される。
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

        // 全候補がクールダウン中の場合は直前ターゲット以外から選ぶ
        if (candidates.Count == 0)
        {
            foreach (var wp in _corridorWaypoints)
            {
                if (wp != _currentTarget)
                    candidates.Add(wp);
            }
        }

        // それでも候補がなければ（ウェイポイントが1つしかない）完全ランダム
        if (candidates.Count == 0)
            return _corridorWaypoints[Random.Range(0, _corridorWaypoints.Count)];

        var selected = WeightedSelectByDistance(candidates);
        _waypointCooldowns[selected] = kWaypointCooldown;
        return selected;
    }

    /// <summary>
    /// 候補リストから現在位置との距離に比例した重みでランダム選出する。
    /// 遠いウェイポイントほど選ばれやすくなり、マップ全体への分散を促す。
    /// </summary>
    private Vector3 WeightedSelectByDistance(List<Vector3> candidates)
    {
        var totalWeight = 0f;
        foreach (var wp in candidates)
            totalWeight += (wp - transform.position).magnitude;

        var threshold = Random.Range(0f, totalWeight);
        var cumulative = 0f;
        foreach (var wp in candidates)
        {
            cumulative += (wp - transform.position).magnitude;
            if (cumulative >= threshold)
                return wp;
        }

        return candidates[candidates.Count - 1];
    }

    /// <summary>
    /// 角詰まり脱出時専用のウェイポイント選出。
    /// 壁法線方向（壁から離れる方向）側にあるウェイポイントのみを候補にして重み付き抽選する。
    /// 候補が0件の場合は通常の重み付き抽選にフォールバックする。
    /// </summary>
    private Vector3 PickEscapeTarget(Vector3 wallNormal)
    {
        var candidates = new List<Vector3>();
        foreach (var wp in _corridorWaypoints)
        {
            var toWp = (wp - transform.position).normalized;
            if (Vector3.Dot(toWp, wallNormal) > 0f)
                candidates.Add(wp);
        }

        if (candidates.Count == 0)
            return PickWeightedWaypoint();

        var selected = WeightedSelectByDistance(candidates);
        _waypointCooldowns[selected] = kWaypointCooldown;
        return selected;
    }

    /// <summary>
    /// 指定したワールド座標へ向かって AddForce で移動する。
    /// WallSlider で壁面に沿うよう補正し、velocity の直接代入は行わない。
    /// </summary>
    private void MoveToward(Vector3 target)
    {
        var diff = target - transform.position;
        var direction = new Vector3(diff.x, 0f, diff.z).normalized;

        if (direction == Vector3.zero) return;

        var slideDir = _wallSlider != null
            ? _wallSlider.SlideDirection(direction)
            : direction;

        if (slideDir == Vector3.zero) return;

        var targetRot = Quaternion.LookRotation(slideDir);
        _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRot, _rotateSpeed * Time.fixedDeltaTime);

        var targetVel = slideDir * _wanderSpeed;
        var currentVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        var velDiff = targetVel - currentVel;

        // 目標速度を超えている方向には Force をかけない（velocity を直接触らずに過剰加速を防ぐ）
        if (Vector3.Dot(velDiff, slideDir) > 0f)
            _rb.AddForce(velDiff * _accelerationForce, ForceMode.Force);
    }
}