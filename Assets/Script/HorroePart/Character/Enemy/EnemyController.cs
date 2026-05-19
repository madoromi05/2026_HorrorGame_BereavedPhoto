using UnityEngine;

/// <summary>
/// Player を追跡する敵の行動を管理するクラス。
/// 徘徊ロジックは IEnemyBehavior（RoomWanderer / MapWanderer）に委譲する。
/// 検知範囲内に Player がいる間は IEnemyBehavior の Tick を止めて追跡に切り替える。
/// 追跡終了時は OnChaseEnded を呼び出し、Behavior 側に状態リセットを通知する。
///
/// 追跡衝突時の停止を防ぐため WallSlider で移動方向を壁面に沿って補正する。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    [Header("追跡")]
    [SerializeField] private float detectRange = 10f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float accelerationForce = 30f;

    [Header("回転")]
    [SerializeField] private float rotateSpeed = 10f;

    private Rigidbody _rb;
    private WallSlider _wallSlider;
    private Transform _player;
    private IEnemyBehavior _wanderBehavior;

    private bool _wasChasingLastFrame;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _wallSlider = GetComponent<WallSlider>();

        if (_wallSlider == null)
            DebugCustom.LogWarning($"[EnemyController] WallSlider が見つかりません: {gameObject.name} → 壁スライド無効");

        _rb.constraints = RigidbodyConstraints.FreezeRotation
                        | RigidbodyConstraints.FreezePositionY;

        _wanderBehavior = (IEnemyBehavior)GetComponent<RoomWanderer>()
                       ?? (IEnemyBehavior)GetComponent<MapWanderer>();

        if (_wanderBehavior == null)
            DebugCustom.LogWarning($"[EnemyController] IEnemyBehavior が見つかりません: {gameObject.name} → 徘徊なし");
    }

    /// <summary>
    /// 外部から Player の Transform を注入する。
    /// EnemySpawner が Instantiate 後に呼び出すこと。
    /// </summary>
    public void SetPlayer(Transform player)
    {
        _player = player;
    }

    private void FixedUpdate()
    {
        if (_player == null) return;

        var isChasing = IsPlayerInRange();

        if (_wasChasingLastFrame && !isChasing)
            _wanderBehavior?.OnChaseEnded();

        _wasChasingLastFrame = isChasing;

        if (isChasing)
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
        ApplyMovement(direction, chaseSpeed);
    }

    /// <summary>
    /// WallSlider で壁面に沿うよう補正してから AddForce で移動する。
    /// velocity の直接代入は AddForce の結果と壁の反発力が干渉するため一切行わない。
    /// 速度差に AddForce の量を掛けることで間接的に制御する。
    /// </summary>
    private void ApplyMovement(Vector3 direction, float speed)
    {
        if (direction == Vector3.zero) return;

        var slideDir = _wallSlider != null
            ? _wallSlider.SlideDirection(direction)
            : direction;

        if (slideDir == Vector3.zero) return;

        var targetRot = Quaternion.LookRotation(slideDir);
        _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime);

        var targetVel = slideDir * speed;
        var currentVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        var diff = targetVel - currentVel;

        // 目標速度を超えている方向には Force をかけない（velocity を直接触らずに過剰加速を防ぐ）
        if (Vector3.Dot(diff, slideDir) > 0f)
            _rb.AddForce(diff * accelerationForce, ForceMode.Force);
    }
}