using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 生成された部屋の内側のみを徘徊する行動クラス。
/// EnemySpawnerからSetRoomBoundsで部屋のAABBを受け取り、
/// 次フレームの位置が境界を超える場合は反射ベクトルで折り返す。
/// Doorグリッドの外縁をAABBの境界とする。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RoomWanderer : MonoBehaviour, IEnemyBehavior
{
    [SerializeField] private float wanderSpeed = 2f;
    [SerializeField] private float wanderInterval = 3f;

    [Header("壁回避 Raycast")]
    [SerializeField] private float rayDistance = 1.5f;

    private Rigidbody _rb;
    private Vector3 _wanderDirection;
    private float _wanderTimer;

    // EnemySpawnerから注入される部屋のワールド空間AABB
    private Bounds _roomBounds;
    private bool _boundsInitialized;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        PickNewDirection();
    }

    /// <summary>
    /// 部屋の移動可能範囲をWorldSpace AABBで受け取る。
    /// EnemySpawnerがInstantiate直後に呼び出すこと。
    /// </summary>
    public void SetRoomBounds(Bounds bounds)
    {
        _roomBounds = bounds;
        _boundsInitialized = true;
    }

    public void Tick()
    {
        _wanderTimer -= Time.fixedDeltaTime;
        if (_wanderTimer <= 0f)
            PickNewDirection();

        // 壁回避
        if (Physics.Raycast(transform.position, _wanderDirection, rayDistance))
            PickNewDirection();

        // 境界チェック：次フレームの予測位置がAABB外なら方向を反転
        if (_boundsInitialized)
            ClampDirectionToBounds();

        MoveToward(_wanderDirection);
    }

    /// <summary>
    /// 次フレームの予測位置がAABB外に出る場合、越境する軸の速度成分を反転する。
    /// </summary>
    private void ClampDirectionToBounds()
    {
        var nextPos = transform.position + _wanderDirection * wanderSpeed * Time.fixedDeltaTime;

        if (nextPos.x < _roomBounds.min.x || nextPos.x > _roomBounds.max.x)
            _wanderDirection.x = -_wanderDirection.x;

        if (nextPos.z < _roomBounds.min.z || nextPos.z > _roomBounds.max.z)
            _wanderDirection.z = -_wanderDirection.z;
    }

    private void PickNewDirection()
    {
        var angle = Random.Range(0f, Mathf.PI * 2f);
        _wanderDirection = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
        _wanderTimer = wanderInterval;
    }

    private void MoveToward(Vector3 direction)
    {
        if (direction == Vector3.zero) return;

        var targetRot = Quaternion.LookRotation(direction);
        _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRot, 10f * Time.fixedDeltaTime);

        var vel = _rb.linearVelocity;
        _rb.linearVelocity = new Vector3(direction.x * wanderSpeed, vel.y, direction.z * wanderSpeed);
    }
}