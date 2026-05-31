using UnityEngine;

/// <summary>
/// 割り当てられた部屋の内部のみを徘徊するクラス。
/// 通常は AABB 内をランダム徘徊し、部屋外に出た場合は部屋中心へ帰還する。
///
/// 追跡（Chase）中は Tick() が呼ばれないため、追跡が AABB を超えた場合に
/// Tick() 再開時に IsOutsideBounds() を判定して帰還ウェイポイントへ切り替える。
///
/// 壁衝突時の停止を防ぐため WallSlider で移動方向を壁面に沿うよう補正する。
/// 角詰まり（2壁に挟まれて完全停止）は WallSlider.IsStuck で検出し、強制的に方向を再抽選する。
/// </summary>
public class RoomWanderer : MonoBehaviour, IEnemyBehavior
{
    [SerializeField] private float _wanderSpeed = 2f;           // 徘徊中移動速度
    [SerializeField] private float _wanderInterval = 3f;        // 新しい方向を選ぶ間隔
    [SerializeField] private float _accelerationForce = 20f;    // 加速度
    [SerializeField] private float _rotateSpeed = 10f;          // 目標方向への回転速度

    [Header("壁回避 Raycast")]
    [SerializeField] private float _rayDistance = 1.5f;         // 壁回避用のレイキャスト距離
    [SerializeField] private float _rayOriginOffset = 0.4f;     // レイキャストの発射位置オフセット
    private Rigidbody _rb;
    private WallSlider _wallSlider;
    private Vector3 _wanderDirection;
    private float _wanderTimer;

    private Bounds _roomBounds;
    private bool _boundsInitialized;

    private bool _isReturning;
    private Vector3 _returnTarget;

    private const float kReturnArrivalRadius = 0.5f;

    // 急 Uターンを防ぐため、新方向の候補角度を現在方向から ±この角度以内に制限する
    private const float kMaxDirectionChangeAngle = 140f;

    // 角詰まり脱出時は壁法線方向を中心にこの角度以内で抽選し、振り子ループを防ぐ
    private const float kEscapeAngleRange = 60f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _wallSlider = GetComponent<WallSlider>();
        PickNewDirection();
    }

    /// <summary>
    /// 部屋の移動可能範囲を WorldSpace AABB で受け取る。
    /// EnemySpawner が Instantiate 後に呼び出すこと。
    /// </summary>
    public void SetRoomBounds(Bounds bounds)
    {
        _roomBounds = bounds;
        _returnTarget = new Vector3(bounds.center.x, transform.position.y, bounds.center.z);
        _boundsInitialized = true;
    }

    /// <summary>
    /// EnemyController が追跡を終了した瞬間に呼び出される。
    /// 部屋外にいる場合は即座に帰還フラグを立てる。
    /// </summary>
    public void OnChaseEnded()
    {
        if (_boundsInitialized && IsOutsideBounds())
            _isReturning = true;
    }

    public void Tick()
    {
        // 角詰まり（2壁に挟まれて速度ゼロが継続）を検出したら壁法線基準で脱出方向を抽選する。
        // 通常の PickNewDirection（±140°）では詰まった方向に再び向かう可能性が高いため、
        // 壁から離れる方向（法線）を中心に ±60° に絞った専用抽選で振り子ループを防ぐ。
        if (_wallSlider != null && _wallSlider.ConsumeStuck(out var escapeNormal))
            PickEscapeDirection(escapeNormal);

        if (!_boundsInitialized)
        {
            WanderFree();
            return;
        }

        if (_isReturning || IsOutsideBounds())
        {
            ReturnToRoom();
            return;
        }

        WanderInsideBounds();
    }

    // 帰還処理

    private bool IsOutsideBounds()
    {
        var pos = transform.position;
        return pos.x < _roomBounds.min.x || pos.x > _roomBounds.max.x
            || pos.z < _roomBounds.min.z || pos.z > _roomBounds.max.z;
    }

    private void ReturnToRoom()
    {
        _isReturning = true;

        var diff = _returnTarget - transform.position;
        var direction = new Vector3(diff.x, 0f, diff.z);

        if (direction.sqrMagnitude <= kReturnArrivalRadius * kReturnArrivalRadius)
        {
            _isReturning = false;
            PickNewDirection();
            return;
        }

        ApplyMovement(direction.normalized);
    }

    // 徘徊処理

    private void WanderInsideBounds()
    {
        _wanderTimer -= Time.fixedDeltaTime;
        if (_wanderTimer <= 0f)
            PickNewDirection();

        var rayOrigin = transform.position + _wanderDirection * _rayOriginOffset;
        if (Physics.Raycast(rayOrigin, _wanderDirection, _rayDistance))
        {
            PickNewDirection();
            ApplyMovement(_wanderDirection);
            return;
        }

        ClampDirectionToBounds();

        if (_wanderDirection.sqrMagnitude < 0.01f)
            PickNewDirection();

        ApplyMovement(_wanderDirection);
    }

    private void WanderFree()
    {
        _wanderTimer -= Time.fixedDeltaTime;
        if (_wanderTimer <= 0f)
            PickNewDirection();

        var rayOrigin = transform.position + _wanderDirection * _rayOriginOffset;
        if (Physics.Raycast(rayOrigin, _wanderDirection, _rayDistance))
            PickNewDirection();

        ApplyMovement(_wanderDirection);
    }

    private void ClampDirectionToBounds()
    {
        var nextPos = transform.position + _wanderDirection * _wanderSpeed * Time.fixedDeltaTime;

        if (nextPos.x < _roomBounds.min.x || nextPos.x > _roomBounds.max.x)
            _wanderDirection.x = -_wanderDirection.x;

        if (nextPos.z < _roomBounds.min.z || nextPos.z > _roomBounds.max.z)
            _wanderDirection.z = -_wanderDirection.z;

        _wanderDirection = _wanderDirection.normalized;
    }

    /// <summary>
    /// 現在の進行方向から ±kMaxDirectionChangeAngle 以内の角度で新しい方向を選ぶ。
    /// 完全ランダムにすると急 Uターンが頻発して動きが不自然になるため範囲を制限している。
    /// </summary>
    private void PickNewDirection()
    {
        var currentAngle = Mathf.Atan2(_wanderDirection.x, _wanderDirection.z) * Mathf.Rad2Deg;
        var newAngle = (currentAngle + Random.Range(-kMaxDirectionChangeAngle, kMaxDirectionChangeAngle)) * Mathf.Deg2Rad;

        _wanderDirection = new Vector3(Mathf.Sin(newAngle), 0f, Mathf.Cos(newAngle));
        _wanderTimer = _wanderInterval;
    }

    /// <summary>
    /// 角詰まり時専用の脱出方向抽選。壁法線（壁から離れる方向）を中心に ±kEscapeAngleRange 以内で選ぶ。
    /// 通常の PickNewDirection より角度範囲を大幅に絞ることで、
    /// 直前に詰まっていた壁方向へ再び向かう振り子ループを防ぐ。
    /// </summary>
    private void PickEscapeDirection(Vector3 wallNormal)
    {
        var baseAngle = Mathf.Atan2(wallNormal.x, wallNormal.z) * Mathf.Rad2Deg;
        var newAngle = (baseAngle + Random.Range(-kEscapeAngleRange, kEscapeAngleRange)) * Mathf.Deg2Rad;

        _wanderDirection = new Vector3(Mathf.Sin(newAngle), 0f, Mathf.Cos(newAngle));
        _wanderTimer = _wanderInterval;
    }

    /// <summary>
    /// WallSlider で壁面に沿うよう補正してから AddForce で移動する。
    /// velocity の直接代入は AddForce の結果と壁の反発力が干渉するため一切行わない。
    /// 速度差に AddForce の量を掛けることで間接的に制御する。
    /// </summary>
    private void ApplyMovement(Vector3 direction)
    {
        if (direction == Vector3.zero) return;

        var slideDir = _wallSlider != null
            ? _wallSlider.SlideDirection(direction)
            : direction;

        if (slideDir == Vector3.zero) return;

        var targetRot = Quaternion.LookRotation(slideDir);
        _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRot, _rotateSpeed * Time.fixedDeltaTime);

        var targetVel = slideDir * _wanderSpeed;
        var currentVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        var diff = targetVel - currentVel;

        // 目標速度を超えている方向には Force をかけない（velocity を直接触らずに過剰加速を防ぐ）
        if (Vector3.Dot(diff, slideDir) > 0f)
            _rb.AddForce(diff * _accelerationForce, ForceMode.Force);
    }
}