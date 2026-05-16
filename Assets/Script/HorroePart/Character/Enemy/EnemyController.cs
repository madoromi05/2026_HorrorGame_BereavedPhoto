using UnityEngine;

/// <summary>
/// Playerを追跡する敵の行動制御。
/// 検知範囲外は Raycast 壁回避付きのランダム徘徊、
/// 検知範囲内は Player へ直進追跡する。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    [Header("追跡")]
    [SerializeField] private float detectRange = 10f;   // この距離以内で追跡開始
    [SerializeField] private float chaseSpeed = 4f;

    [Header("徘徊")]
    [SerializeField] private float wanderSpeed = 2f;
    [SerializeField] private float wanderInterval = 3f; // 次の徘徊方向を決めるまでの秒数

    [Header("壁回避 Raycast")]
    [SerializeField] private float rayDistance = 1.5f;  // 前方壁感知距離
    [SerializeField] private float avoidAngle  = 45f;    // 壁検知時の回避角度

    [Header("その他")]
    [SerializeField] private float rotateSpeed = 10f;   // 向き補間速度

    private Rigidbody _rb;
    private Transform _player;
    private Vector3 _wanderDirection;
    private float _wanderTimer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // Y軸回転のみ許可（横倒し防止）
        _rb.constraints = RigidbodyConstraints.FreezeRotation
                        | RigidbodyConstraints.FreezePositionY;
    }

    /// <summary>
    /// 外部から Player の Transform を注入する。
    /// DungeonGridBuilder など実行時生成側から呼ぶこと。
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
            Wander();
    }

    // 追跡
    private bool IsPlayerInRange()
    {
        return (_player.position - transform.position).sqrMagnitude <= detectRange * detectRange;
    }

    private void Chase()
    {
        var direction = (_player.position - transform.position).WithY(0f).normalized;
        Move(direction, chaseSpeed);
    }

    // 徘徊
    private void Wander()
    {
        _wanderTimer -= Time.fixedDeltaTime;
        if (_wanderTimer <= 0f)
            PickNewWanderDirection();
        var direction = AvoidWall(_wanderDirection);
        // AvoidWall が回避不能と判断した場合、即座に再抽選して反映する
        if (direction == Vector3.zero)
            PickNewWanderDirection();
        Move(_wanderDirection, wanderSpeed);
    }

    private void PickNewWanderDirection()
    {
        var angle = Random.Range(0f, Mathf.PI * 2f);
        _wanderDirection = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
        _wanderTimer = wanderInterval;
    }

    // 壁回避
    /// <summary>
    /// 前方に壁があれば avoidAngle 分だけ右に回避した方向を返す。
    /// 壁がなければそのまま返す。
    /// </summary>
    private Vector3 AvoidWall(Vector3 direction)
    {
        if (!Physics.Raycast(transform.position, direction, rayDistance))
            return direction;
        var avoided = Quaternion.Euler(0f, avoidAngle, 0f) * direction;
        // 回避先も壁なら呼び出し元に再抽選を委ねる
        if (Physics.Raycast(transform.position, avoided, rayDistance))
            return Vector3.zero;
        return avoided;
    }

    // 移動・回転
    private void Move(Vector3 direction, float speed)
    {
        if (direction == Vector3.zero) return;

        // 向きを滑らかに補間
        var targetRot = Quaternion.LookRotation(direction);
        _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime);

        var vel = _rb.linearVelocity;
        _rb.linearVelocity = new Vector3(direction.x * speed, vel.y, direction.z * speed);
    }
}

/// <summary>
/// Vector3 の Y 成分だけ置換する拡張。
/// </summary>
public static class Vector3Extensions
{
    public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
}