using System.Collections.Generic;
using DungeonSystem;
using UnityEngine;

/// <summary>
/// 通路セル（GridType.Corridor / Door）のワールド座標をウェイポイントとして順に移動する徘徊クラス。
/// EnemySpawner から SetGrid で通路座標リストを受け取り、重み付き抽選で次の目標を選び続ける。
/// 部屋にとどまらず通路を優先的に歩き回ることを目的としている。
///
/// マップ全体を巡回しやすくするため以下の3つの仕組みを持つ。
/// 1. 直前のターゲットを除外して連続同一選択を防ぐ。
/// 2. 遠いウェイポイントほど選ばれやすい距離重み付き抽選で偏りを抑える。
/// 3. 一度選んだウェイポイントに kWaypointCooldown 秒のクールダウンを設けて短期間の行き来を防ぐ。
///
/// 壁衝突時は WallSlider で進行方向を反転し、Behavior 側の ConsumeStuck で脱出方向を決定する。
/// </summary>
public class MapWanderer : MonoBehaviour, IEnemyBehavior
{
    [SerializeField] private float _wanderSpeed = 4f;           // 徘徊中移動速度
    [SerializeField] private float _rotateSpeed = 10f;          // 目標方向への回転速度
    [SerializeField] private float _accelerationForce = 20f;    // 加速度
    [SerializeField] private float _arrivalRadius = 1.0f;       // 到着判定半径

    private Rigidbody _rb;
    private WallSlider _wallSlider;

    private List<Vector3> _corridorWaypoints = new List<Vector3>();
    private Vector3 _currentTarget;
    private bool _isInitialized;

    // ウェイポイントごとの残りクールダウン時間。0以下なら選出可能
    private Dictionary<Vector3, float> _waypointCooldowns = new Dictionary<Vector3, float>();

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

        // クールダウンをカウントダウンする
        var keys = new List<Vector3>(_waypointCooldowns.Keys);
        foreach (var key in keys)
        {
            _waypointCooldowns[key] -= Time.fixedDeltaTime;
            if (_waypointCooldowns[key] <= 0f)
                _waypointCooldowns.Remove(key);
        }
    }

    /// <summary>
    /// グリッドを受け取り、Corridor・Door セルのウェイポイント XZ 座標リストを構築する。
    /// Door は Corridor と部屋の境界セルであり、経路上でウェイポイントを分断させず
    /// 隣接した次の目標を目指せる出口路として含める。
    /// EnemySpawner が Instantiate 後に呼び出すこと。
    /// </summary>
    public void SetGrid(GridType[,] grid, float gridSize)
    {
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
        _isInitialized = true;
    }

    /// <summary>
    /// MapWanderer は部屋範囲を持たないため追跡終了時に特別な処理は不要。
    /// IEnemyBehavior の契約を満たすために空実装として定義する。
    /// </summary>
    public void OnChaseEnded() { }

    public void Tick()
    {
        if (!_isInitialized) return;

        // 角詰まり（2壁に挟まれて速度ゼロが継続）を検出したら壁法線基準で脱出ウェイポイントを選ぶ。
        // 単純な再抽選では詰まった壁方向のウェイポイントが選ばれて振り子ループになるため、
        // 壁から離れる方向（法線）に近い側のウェイポイントに絞って選出する。
        if (_wallSlider != null && _wallSlider.ConsumeStuck(out var escapeNormal))
            _currentTarget = PickEscapeTarget(escapeNormal);

        if (IsArrived())
            _currentTarget = PickWeightedWaypoint();

        MoveTowardTarget();
    }

    private bool IsArrived()
    {
        var diff = _currentTarget - transform.position;
        return diff.x * diff.x + diff.z * diff.z <= _arrivalRadius * _arrivalRadius;
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
    /// WallSlider で壁に当たった場合は進行方向を反転してから AddForce で移動する。
    /// velocity の直接代入は AddForce の結果と壁の反発力が干渉するため一切行わない。
    /// 速度差に AddForce の量を掛けることで間接的に制御する。
    /// </summary>
    private void MoveTowardTarget()
    {
        var diff = _currentTarget - transform.position;
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