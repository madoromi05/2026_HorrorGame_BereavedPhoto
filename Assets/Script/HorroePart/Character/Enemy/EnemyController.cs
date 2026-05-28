using UnityEngine;

/// <summary>
/// Player を追跡する敵の行動を管理するクラス。
/// 徘徊ロジックは IEnemyBehavior（RoomWanderer / MapWanderer）に委譲する。
/// 検知範囲内に Player がいる間は IEnemyBehavior の Tick を止めて追跡に切り替える。
/// 追跡終了時は OnChaseEnded を呼び出し、Behavior 側に状態リセットを通知する。
///
/// 追跡衝突時の停止を防ぐために WallSlider で移動方向を壁面に合わせて補正する。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    [Header("ステルス検知")]
    [SerializeField] private float _lightDetectRange = 20f;
    [SerializeField] private float _lightDetectAngle = 60f;

    [Header("追跡")]
    [SerializeField] private float _detectRange = 10f;
    [SerializeField] private float _chaseSpeed = 4f;
    [SerializeField] private float _accelerationForce = 30f;

    [Header("回転")]
    [SerializeField] private float _rotateSpeed = 10f;

    private Rigidbody _rb;
    private WallSlider _wallSlider;
    private Transform _player;
    private IEnemyBehavior _wanderBehavior;
    private GameOverHandler _gameOverHandler;
    private bool _wasChasingLastFrame;
    private PlayerStealthStatus _playerStealth;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _wallSlider = GetComponent<WallSlider>();

        if (_wallSlider == null)
            DebugCustom.LogWarning($"[EnemyController] WallSliderが見つかりません: {gameObject.name} → 壁スライドなし");

        _rb.constraints = RigidbodyConstraints.FreezeRotation
                        | RigidbodyConstraints.FreezePositionY;

        _wanderBehavior = (IEnemyBehavior)GetComponent<RoomWanderer>()
                       ?? (IEnemyBehavior)GetComponent<MapWanderer>();

        if (_wanderBehavior == null)
            DebugCustom.LogWarning($"[EnemyController] IEnemyBehaviorが見つかりません: {gameObject.name} → 徘徊なし");
    }

    /// <summary>
    /// 外部から Player の Transform を注入する。
    /// EnemySpawner が Instantiate 後に呼び出すこと。
    /// </summary>
    public void SetPlayer(Transform player)
    {
        _player = player;
        _gameOverHandler = _player.GetComponent<GameOverHandler>();

        if (_gameOverHandler == null)
        {
            DebugCustom.LogWarning($"[EnemyController] GameOverHandlerが見つかりません: {_player.name}");
        }
    }

    private void FixedUpdate()
    {
        if (_player == null) return;

        var isChasing = IsPlayerDetected();

        if (_wasChasingLastFrame && !isChasing)
            _wanderBehavior?.OnChaseEnded();

        _wasChasingLastFrame = isChasing;

        if (isChasing)
            Chase();
        else
            _wanderBehavior?.Tick();
    }

    private bool IsPlayerDetected()
    {
        float sqDist = (_player.position - transform.position).sqrMagnitude;

        // 通常の近接検知（距離のみ）
        if (sqDist <= _detectRange * _detectRange) return true;

        if (_playerStealth == null) return false;

        // 足音検知（移動時のノイズ半径内に入ったとき）
        float noise = _playerStealth.FootstepNoiseRadius;
        if (noise > 0f && sqDist <= noise * noise) return true;

        // ライト検知（ライトON時は範囲内かつ正面方向に存在するとき）
        if (_playerStealth.IsLightOn && sqDist <= _lightDetectRange * _lightDetectRange)
        {
            Vector3 toEnemy = (transform.position - _player.position).normalized;
            float angle = Vector3.Angle(_player.forward, toEnemy);
            if (angle <= _lightDetectAngle) return true;
        }

        return false;
    }

    private void Chase()
    {
        var direction = new Vector3(
            _player.position.x - transform.position.x,
            0f,
            _player.position.z - transform.position.z
        ).normalized;
        ApplyMovement(direction, _chaseSpeed);
    }

    /// <summary>
    /// WallSliderで壁面に合わせて補正してから AddForce で移動する。
    /// velocityの直接操作は AddForce の結果と壁の反力が干渉するため一切行わない。
    /// 速度を AddForce の量に掛けることで間接的に制御する。
    /// </summary>
    private void ApplyMovement(Vector3 direction, float speed)
    {
        if (direction == Vector3.zero) return;

        var slideDir = _wallSlider != null
            ? _wallSlider.SlideDirection(direction)
            : direction;

        if (slideDir == Vector3.zero) return;

        var targetRot = Quaternion.LookRotation(slideDir);
        _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRot, _rotateSpeed * Time.fixedDeltaTime);

        var targetVel = slideDir * speed;
        var currentVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        var diff = targetVel - currentVel;

        // 目標速度を超えている方向には Force を加えない（velocityを直接触らずに過速を防ぐ）
        if (Vector3.Dot(diff, slideDir) > 0f)
            _rb.AddForce(diff * _accelerationForce, ForceMode.Force);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject != _player.gameObject) return;
        _gameOverHandler?.TriggerGameOver();
    }
}
