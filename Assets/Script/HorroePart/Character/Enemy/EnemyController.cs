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
    [Header("ステルス検知")]
    [SerializeField] private float lightDetectRange = 20f;
    [SerializeField] private float lightDetectAngle = 60f;

    [Header("追跡")]
    [SerializeField] private float detectRange = 10f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float accelerationForce = 30f;

    [Header("回転")]
    [SerializeField] private float rotateSpeed = 10f;

    private Rigidbody rb;
    private WallSlider wallSlider;
    private Transform player;
    private IEnemyBehavior wanderBehavior;
    private GameOverHandler gameOverHandler;
    private bool wasChasingLastFrame;
    private PlayerStealthStatus playerStealth;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        wallSlider = GetComponent<WallSlider>();

        if (wallSlider == null)
            DebugCustom.LogWarning($"[EnemyController] WallSlider が見つかりません: {gameObject.name} → 壁スライド無効");

        rb.constraints = RigidbodyConstraints.FreezeRotation
                        | RigidbodyConstraints.FreezePositionY;

        wanderBehavior = (IEnemyBehavior)GetComponent<RoomWanderer>()
                       ?? (IEnemyBehavior)GetComponent<MapWanderer>();

        if (wanderBehavior == null)
            DebugCustom.LogWarning($"[EnemyController] IEnemyBehavior が見つかりません: {gameObject.name} → 徘徊なし");
    }

    /// <summary>
    /// 外部から Player の Transform を注入する。
    /// EnemySpawner が Instantiate 後に呼び出すこと。
    /// </summary>
    public void SetPlayer(Transform _player)
    {
        player = _player;
        gameOverHandler = player.GetComponent<GameOverHandler>();

        if (gameOverHandler == null)
        {
            DebugCustom.LogWarning($"[EnemyController] GameOverHandler が見つかりません: {player.name}");

        }
    }

    private void FixedUpdate()
    {
        if (player == null) return;

        var _isChasing = IsPlayerDetected();

        if (wasChasingLastFrame && !_isChasing)
            wanderBehavior?.OnChaseEnded();

        wasChasingLastFrame = _isChasing;

        if (_isChasing)
            Chase();
        else
            wanderBehavior?.Tick();
    }

    private bool IsPlayerDetected()
    {
        float _sqDist = (player.position - transform.position).sqrMagnitude;

        // 通常の近接検知（従来通り）
        if (_sqDist <= detectRange * detectRange) return true;

        if (playerStealth == null) return false;

        // 足音検知（移動中のノイズ半径内に入ったら）
        float noise = playerStealth.FootstepNoiseRadius;
        if (noise > 0f && _sqDist <= noise * noise) return true;

        // ライト検知（ライトがONかつ一定範囲内、かつ自分の方向を向いている）
        if (playerStealth.IsLightOn && _sqDist <= lightDetectRange * lightDetectRange)
        {
            Vector3 _toEnemy = (transform.position - player.position).normalized;
            float _angle = Vector3.Angle(player.forward, _toEnemy);
            if (_angle <= lightDetectAngle) return true;
        }

        return false;
    }

    private void Chase()
    {
        var _direction = new Vector3(
            player.position.x - transform.position.x,
            0f,
            player.position.z - transform.position.z
        ).normalized;
        ApplyMovement(_direction, chaseSpeed);
    }

    /// <summary>
    /// WallSlider で壁面に沿うよう補正してから AddForce で移動する。
    /// velocity の直接代入は AddForce の結果と壁の反発力が干渉するため一切行わない。
    /// 速度差に AddForce の量を掛けることで間接的に制御する。
    /// </summary>
    private void ApplyMovement(Vector3 _direction, float _speed)
    {
        if (_direction == Vector3.zero) return;

        var _slideDir = wallSlider != null
            ? wallSlider.SlideDirection(_direction)
            : _direction;

        if (_slideDir == Vector3.zero) return;

        var _targetRot = Quaternion.LookRotation(_slideDir);
        rb.rotation = Quaternion.Slerp(rb.rotation, _targetRot, rotateSpeed * Time.fixedDeltaTime);

        var _targetVel = _slideDir * _speed;
        var _currentVel = new Vector3(rb.rotation.x, 0f, rb.rotation.z);
        var _diff = _targetVel - _currentVel;

        // 目標速度を超えている方向には Force をかけない（velocity を直接触らずに過剰加速を防ぐ）
        if (Vector3.Dot(_diff, _slideDir) > 0f)
            rb.AddForce(_diff * accelerationForce, ForceMode.Force);
    }

    private void OnCollisionEnter(Collision _collision)
    {
        if (_collision.gameObject != player.gameObject) return;
        gameOverHandler?.TriggerGameOver();
    }
}