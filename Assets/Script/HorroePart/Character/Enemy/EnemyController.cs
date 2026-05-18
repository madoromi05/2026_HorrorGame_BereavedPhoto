using UnityEngine;

/// <summary>
/// Playerを追跡する敵の行動制御クラス。
/// 徘徊ロジックはIEnemyBehavior（RoomWanderer / MapWanderer）に委譲する。
/// 検知範囲内にPlayerがいる間はIEnemyBehaviorのTickを停止して追跡に切り替える。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    [Header("追跡")]
    [SerializeField] private float detectRange = 10f;
    [SerializeField] private float chaseSpeed = 4f;

    [Header("その他")]
    [SerializeField] private float rotateSpeed = 10f;

    private Rigidbody _rb;
    private Transform _player;
    private IEnemyBehavior _wanderBehavior;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotation
                        | RigidbodyConstraints.FreezePositionY;

        // 徘徊行動はPrefabにアタッチされたコンポーネントから取得する
        _wanderBehavior = (IEnemyBehavior)GetComponent<RoomWanderer>()
                       ?? (IEnemyBehavior)GetComponent<MapWanderer>();
    }

    /// <summary>
    /// 外部からPlayerのTransformを注入する。
    /// EnemySpawnerがInstantiate直後に呼び出すこと。
    /// </summary>
    public void SetPlayer(Transform player)
    {
        _player = player;
    }

    private void FixedUpdate()
    {
        if (_player == null) return;

        if (IsPlayerInRange())
            Chase();
        else
            _wanderBehavior?.Tick();
    }

    private bool IsPlayerInRange()
    {
        return (_player.position - transform.position).sqrMagnitude <= detectRange * detectRange;
    }

    private void Chase()
    {
        var direction = new Vector3(
            _player.position.x - transform.position.x,
            0f,
            _player.position.z - transform.position.z
        ).normalized;
        Move(direction, chaseSpeed);
    }

    private void Move(Vector3 direction, float speed)
    {
        if (direction == Vector3.zero) return;

        var targetRot = Quaternion.LookRotation(direction);
        _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime);

        var vel = _rb.linearVelocity;
        _rb.linearVelocity = new Vector3(direction.x * speed, vel.y, direction.z * speed);
    }
}