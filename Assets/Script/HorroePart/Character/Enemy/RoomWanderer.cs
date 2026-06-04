using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NavMeshAgent を使って割り当てられた部屋の内部のみを徘徊するクラス。
/// NavMesh.SamplePosition で部屋 AABB 内のランダム点を選び SetDestination で移動する。
/// 部屋外に出た場合は部屋中央へ帰還する。
/// </summary>
public class RoomWanderer : MonoBehaviour, IEnemyBehavior
{
    [SerializeField] private float _wanderSpeed    = 2f;
    [SerializeField] private float _wanderInterval = 3f;

    private NavMeshAgent _agent;
    private Bounds _roomBounds;
    private bool   _boundsInitialized;
    private float  _wanderTimer;
    private bool   _isReturning;

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
            if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
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
        if (_wanderTimer <= 0f || (!_agent.pathPending && _agent.remainingDistance < 0.3f))
            PickNewDestination();
    }

    private void PickNewDestination()
    {
        var candidate = new Vector3(
            Random.Range(_roomBounds.min.x, _roomBounds.max.x),
            transform.position.y,
            Random.Range(_roomBounds.min.z, _roomBounds.max.z)
        );

        if (NavMesh.SamplePosition(candidate, out var hit, _roomBounds.extents.magnitude, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);

        _wanderTimer = _wanderInterval;
    }

    private bool IsOutsideBounds()
    {
        var pos = transform.position;
        return pos.x < _roomBounds.min.x || pos.x > _roomBounds.max.x
            || pos.z < _roomBounds.min.z || pos.z > _roomBounds.max.z;
    }
}
