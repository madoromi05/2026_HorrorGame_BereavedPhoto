using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NavMeshAgent を使って割り当てられた部屋の内部のみを徘徊するクラス。
/// NavMesh.SamplePosition で部屋 AABB 内のランダム点を選び SetDestination で移動する。
/// 部屋外に出た場合は部屋中央へ帰還する。
/// </summary>
public class RoomWanderer : MonoBehaviour, IEnemyBehavior
{
    [Header("敵種別")]
    [SerializeField] private EnemyType _enemyType = EnemyType.Mother;
    public EnemyType EnemyType => _enemyType;

    [Header("徘徊設定")]
    [SerializeField] private float _wanderSpeed    = 2f;
    [SerializeField] private float _wanderInterval = 3f;

    private NavMeshAgent _agent;
    private Bounds _roomBounds;
    private bool   _boundsInitialized;
    private float  _wanderTimer;
    private bool   _isReturning;

    // 直近 3 箇所の訪問履歴（同じ領域を周回しないために使う）
    private const int kHistorySize = 3;
    private readonly Vector3[] _posHistory = new Vector3[kHistorySize];
    private int _historyCount;
    private int _historyHead;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    public void SetRoomBounds(Bounds bounds)
    {
        _roomBounds         = bounds;
        _boundsInitialized  = true;
    }

    public void OnChaseEnded()
    {
        if (_boundsInitialized && IsOutsideBounds())
        {
            _isReturning = true;
            _agent.SetDestination(_roomBounds.center);
        }
    }

    public void Tick()
    {
        if (!_boundsInitialized || _agent == null) return;

        _agent.speed = _wanderSpeed;

        if (_isReturning)
        {
            // hasPath が false かつ pathPending が false = パス失敗 → そのまま巡回再開
            bool reachedOrFailed = !_agent.pathPending &&
                (!_agent.hasPath || _agent.remainingDistance < 0.5f);
            if (reachedOrFailed)
            {
                _isReturning = false;
                PickNewDestination();
            }
            return;
        }

        if (IsOutsideBounds())
        {
            _isReturning = true;
            _agent.SetDestination(_roomBounds.center);
            return;
        }

        _wanderTimer -= Time.deltaTime;
        // hasPath チェックを加えることで「パス未確立時の remainingDistance==0」誤発火を防ぐ
        bool arrived = _agent.hasPath && !_agent.pathPending && _agent.remainingDistance < 0.3f;
        if (_wanderTimer <= 0f || arrived)
            PickNewDestination();
    }

    private void PickNewDestination()
    {
        const float kSampleRadius = 2f;
        Vector3 best = Vector3.zero;
        float bestScore = -1f;

        // 10 個の候補を生成し、直近訪問履歴から最も遠い点を採用する
        for (int i = 0; i < 10; i++)
        {
            var candidate = new Vector3(
                Random.Range(_roomBounds.min.x, _roomBounds.max.x),
                transform.position.y,
                Random.Range(_roomBounds.min.z, _roomBounds.max.z)
            );
            if (!NavMesh.SamplePosition(candidate, out var hit, kSampleRadius, NavMesh.AllAreas)) continue;
            if (IsOutsidePoint(hit.position)) continue;

            float score = MinDistToHistory(hit.position);
            if (score > bestScore) { bestScore = score; best = hit.position; }
        }

        if (bestScore >= 0f)
        {
            _agent.SetDestination(best);
            RecordHistory(best);
        }
        _wanderTimer = _wanderInterval;
    }

    private float MinDistToHistory(Vector3 pos)
    {
        if (_historyCount == 0) return float.MaxValue;
        float min = float.MaxValue;
        for (int i = 0; i < _historyCount; i++)
            min = Mathf.Min(min, (pos - _posHistory[i]).sqrMagnitude);
        return min;
    }

    private void RecordHistory(Vector3 pos)
    {
        _posHistory[_historyHead] = pos;
        _historyHead = (_historyHead + 1) % kHistorySize;
        if (_historyCount < kHistorySize) _historyCount++;
    }

    private bool IsOutsideBounds() => IsOutsidePoint(transform.position);

    private bool IsOutsidePoint(Vector3 p)
    {
        return p.x < _roomBounds.min.x || p.x > _roomBounds.max.x
            || p.z < _roomBounds.min.z || p.z > _roomBounds.max.z;
    }
}
