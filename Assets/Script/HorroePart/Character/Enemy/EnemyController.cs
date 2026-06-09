using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NavMeshAgent を使った敵AI。
/// EnemyPerception で警戒度を更新し、多段ステートマシンで行動を切り替える。
/// 移動は NavMeshAgent.SetDestination() に委譲する。
/// Rigidbody・WallSlider・DungeonPathfinder は不要になる。
///
///   Patrol     : 通常巡回（IEnemyBehavior.Tick）
///   Suspicious : 警戒。最後に知られた地点へゆっくり接近
///   Chase      : 追跡（完全発見）
///   Search     : 見失い後、最後に見た地点周辺をうろつく
///   Feint      : 捜索を切り上げて立ち去る素振り。確率で振り返って再捜索
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    public enum AIState { Patrol, Suspicious, Chase, Search, Feint }

    [Header("起動距離")]
    [SerializeField] private float _activationRadius = 20f;

    [Header("追跡")]
    [SerializeField] private float _chaseSpeed = 6f;
    [SerializeField] private float _chaseDestUpdateInterval = 0.6f;
    [SerializeField] private float _catchDistance = 1.2f;

    [Header("速度（巡回速度は各 Wanderer コンポーネントで設定）")]
    [SerializeField] private float _suspiciousSpeed = 1.5f;
    [SerializeField] private float _searchSpeed     = 3f;

    [Header("捜索/警戒")]
    [SerializeField] private float _searchDuration       = 20f;
    [SerializeField] private float _searchRoamRadius     = 4f;
    [SerializeField] private float _searchRepickInterval = 3f;
    [SerializeField] private float _feintDuration        = 4f;
    [Range(0f, 1f)]
    [SerializeField] private float _feintReturnChance    = 0.5f;

    private NavMeshAgent    _agent;
    private EnemyPerception _detector;
    private Transform       _player;
    private IEnemyBehavior  _wanderBehavior;
    private GameOverHandler _gameOverHandler;

    private AIState _state = AIState.Patrol;

    // 追跡
    private float _chaseDestTimer;

    // 捜索
    private Vector3 _searchCenter;
    private float   _searchTimer;
    private float   _searchRepickTimer;
    private float   _currentRoamRadius;

    // フェイント
    private float _feintTimer;
    private bool  _feintWillReturn;

    public AIState State => _state;

    private void Awake()
    {
        _agent    = GetComponent<NavMeshAgent>();
        _detector = GetComponent<EnemyPerception>() ?? gameObject.AddComponent<EnemyPerception>();

        _wanderBehavior = (IEnemyBehavior)GetComponent<RoomWanderer>()
                       ?? (IEnemyBehavior)GetComponent<MapWanderer>();

        if (_wanderBehavior == null)
            DebugCustom.LogWarning($"[EnemyController] IEnemyBehaviorが見つかりません: {gameObject.name} → 徘徊なし");
    }

    public void SetPlayer(Transform player)
    {
        _player = player;
        _gameOverHandler = _player.GetComponent<GameOverHandler>();
        _detector?.SetPlayer(player);
    }

    private void Update()
    {
        if (_player == null)
        {
            DebugCustom.LogWarning($"[EnemyController] {gameObject.name}: _player が未設定です。");
            return;
        }

        // 捕捉判定：状態に関わらず近距離なら即ゲームオーバー
        if (_gameOverHandler != null)
        {
            var dx = _player.position.x - transform.position.x;
            var dz = _player.position.z - transform.position.z;
            if (dx * dx + dz * dz < _catchDistance * _catchDistance)
            {
                _gameOverHandler.TriggerGameOver(transform);
                return;
            }
        }

        // 起動距離チェック：遠方では巡回のみ継続
        var pdx = _player.position.x - transform.position.x;
        var pdz = _player.position.z - transform.position.z;
        if (pdx * pdx + pdz * pdz > _activationRadius * _activationRadius)
        {
            if (_state == AIState.Patrol)
                _wanderBehavior?.Tick();
            return;
        }

        _detector.UpdateAwareness(Time.deltaTime);
        float awareness = _detector.AwarenessLevel;

        switch (_state)
        {
            case AIState.Patrol:     TickPatrol(awareness);     break;
            case AIState.Suspicious: TickSuspicious(awareness); break;
            case AIState.Chase:      TickChase(awareness);      break;
            case AIState.Search:     TickSearch(awareness);     break;
            case AIState.Feint:      TickFeint(awareness);      break;
        }
    }

    // ---------------- 各状態 ----------------

    private void TickPatrol(float awareness)
    {
        if (awareness >= 1f)                            { EnterChase(); return; }
        if (awareness >= _detector.SuspicionThreshold) { _state = AIState.Suspicious; return; }
        _wanderBehavior?.Tick();
    }

    private void TickSuspicious(float awareness)
    {
        if (awareness >= 1f)                           { EnterChase(); return; }
        if (awareness < _detector.SuspicionThreshold) { EndToPatrol(); return; }

        _agent.speed = _suspiciousSpeed;
        var target = _detector.HasLastKnown
            ? _detector.LastKnownPosition
            : transform.position + _detector.LastStimulusDirection;
        _agent.SetDestination(target);
    }

    private void TickChase(float awareness)
    {
        if (awareness < 1f) { BeginSearch(); return; }

        _agent.speed = _chaseSpeed;
        _chaseDestTimer -= Time.deltaTime;
        if (_chaseDestTimer <= 0f)
        {
            _agent.SetDestination(_player.position);
            _chaseDestTimer = _chaseDestUpdateInterval;
        }
    }

    private void TickSearch(float awareness)
    {
        if (awareness >= 1f) { EnterChase(); return; }

        _agent.speed = _searchSpeed;
        _searchTimer -= Time.deltaTime;
        if (_searchTimer <= 0f) { BeginFeint(); return; }

        _searchRepickTimer -= Time.deltaTime;
        bool arrived = !_agent.pathPending && _agent.remainingDistance < 1f;
        if (arrived || _searchRepickTimer <= 0f)
            PickSearchPoint();
    }

    private void TickFeint(float awareness)
    {
        if (awareness >= 1f) { EnterChase(); return; }

        _feintTimer -= Time.deltaTime;
        if (_feintTimer > 0f) return;

        if (_feintWillReturn)
        {
            _feintWillReturn   = false;
            _searchTimer       = _searchDuration * 0.5f;
            _searchRepickTimer = 0f;
            _currentRoamRadius = _searchRoamRadius * 0.5f;
            _state = AIState.Search;
            PickSearchPoint();
            return;
        }
        EndToPatrol();
    }

    // ---------------- 状態遷移ヘルパー ----------------

    private void EnterChase()
    {
        _state = AIState.Chase;
        _chaseDestTimer = 0f;
        _agent.speed = _chaseSpeed;
        _agent.SetDestination(_player.position);
        AudioManager.Instance?.PlayBgm(BgmType.GameChase);
    }

    private void BeginSearch()
    {
        _searchCenter      = _detector.HasLastKnown ? _detector.LastKnownPosition : transform.position;
        _searchTimer       = _searchDuration;
        _searchRepickTimer = 0f;
        _currentRoamRadius = _searchRoamRadius * 0.5f;
        _state = AIState.Search;
        PickSearchPoint();
        SwitchToNormalBgmIfSafe();
    }

    private void PickSearchPoint()
    {
        _currentRoamRadius = Mathf.Min(_currentRoamRadius + 0.5f, _searchRoamRadius);
        var r = Random.insideUnitCircle * _currentRoamRadius;
        var candidate = new Vector3(_searchCenter.x + r.x, transform.position.y, _searchCenter.z + r.y);

        if (NavMesh.SamplePosition(candidate, out var hit, _searchRoamRadius * 2f, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);

        _searchRepickTimer = _searchRepickInterval;
    }

    private void BeginFeint()
    {
        _feintTimer      = _feintDuration;
        _feintWillReturn = Random.value < _feintReturnChance;
        _state           = AIState.Feint;

        _agent.speed = _suspiciousSpeed;
        var away = transform.position - _searchCenter;
        away.y = 0f;
        var dir = away.sqrMagnitude > 0.01f ? away.normalized : transform.forward;

        if (NavMesh.SamplePosition(transform.position + dir * 5f, out var hit, 8f, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
    }

    private void EndToPatrol()
    {
        _wanderBehavior?.OnChaseEnded();
        _state = AIState.Patrol;
        SwitchToNormalBgmIfSafe();
    }

    private void SwitchToNormalBgmIfSafe()
    {
        foreach (var e in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            if (e != this && e.State == AIState.Chase) return;
        AudioManager.Instance?.PlayBgm(BgmType.GameNormal);
    }
}
