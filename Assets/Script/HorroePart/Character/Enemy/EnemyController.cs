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
    // ---------------------------------------------------------------
    // 検知条件まとめ（IsPlayerDetected の評価順）
    //
    //  1. 近接キャッチ  : 距離 <= _detectRange (2m)
    //                     → 背後・壁越し関係なく即検知。体当たり防止用
    //
    //  2. 視界（FOVコーン + レイキャスト）
    //                   : 距離 <= _sightRange (13m)
    //                     かつ 正面からの角度 <= _sightHalfAngle (35°) ＝全体70°
    //                     かつ 壁レイヤーにレイが当たらない（遮蔽なし）
    //                     → 正面の視野内でかつ壁で隠れていないとき検知
    //
    //  3. 音検知        : FootstepNoiseRadius > 0
    //                     かつ 距離 <= FootstepNoiseRadius（ダッシュ10m / 歩き5m / しゃがみ1.5m）
    //                     → 足音の大きさに応じた半径内にいるとき検知
    //
    //  4. ライト検知    : 懐中電灯ON
    //                     かつ 距離 <= _lightDetectRange (20m)
    //                     かつ プレイヤー正面から敵方向の角度 <= _lightDetectAngle (60°)
    //                     → 懐中電灯で敵の方向を照らしているとき検知
    // ---------------------------------------------------------------

    [Header("視界検知")]
    [SerializeField] private float _sightRange = 8f;
    [SerializeField] private float _sightHalfAngle = 35f;
    [SerializeField] private LayerMask _sightLayerMask;
    [SerializeField] private float _eyeHeightOffset = 1.5f;

    [Header("ライト検知（ステルス）")]
    [SerializeField] private float _lightDetectRange = 20f;
    [SerializeField] private float _lightDetectAngle = 60f;

    [Header("追跡")]
    [SerializeField] private float _detectRange = 2f;
    [SerializeField] private float _chaseSpeed = 6f;
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
        _playerStealth = _player.GetComponent<PlayerStealthStatus>(); // ← この1行を追加

        if (_gameOverHandler == null)
            DebugCustom.LogWarning($"[EnemyController] GameOverHandlerが見つかりません: {_player.name}");
        if (_playerStealth == null)
            DebugCustom.LogWarning($"[EnemyController] PlayerStealthStatusが見つかりません: {_player.name}");
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
        Vector3 diff = _player.position - transform.position;
        float sqDist = diff.sqrMagnitude;

        // ① 近接キャッチ
        //    距離 <= _detectRange (2m) なら角度・遮蔽に関係なく即検知。
        //    プレイヤーが背後から接触した場合もゲームオーバーにするための安全網。
        if (sqDist <= _detectRange * _detectRange) return true;

        // ② 視界（FOVコーン + レイキャスト）
        //    距離が _sightRange (13m) 以内かつ
        //    敵の正面から _sightHalfAngle (35°) 以内の視野角にプレイヤーがいて、
        //    かつ _sightLayerMask で指定した壁にレイが当たらない（視線が通っている）とき検知。
        if (sqDist <= _sightRange * _sightRange)
        {
            float angle = Vector3.Angle(transform.forward, diff.normalized);
            if (angle <= _sightHalfAngle)
            {
                Vector3 origin = transform.position + Vector3.up * _eyeHeightOffset; // 敵の目線
                Vector3 target = _player.position  + Vector3.up * 1f;               // プレイヤー中心
                float dist = Vector3.Distance(origin, target);
                // Raycast が false = 遮蔽物なし = 視線通り = 検知
                if (!Physics.Raycast(origin, (target - origin).normalized, dist, _sightLayerMask))
                    return true;
            }
        }

        if (_playerStealth == null) return false;

        // ③ 音検知
        //    PlayerStealthStatus が計算した FootstepNoiseRadius（足音の聞こえる半径）が
        //    プレイヤーと敵の距離以上なら検知。
        //    ダッシュ 10m / 歩き 5m / しゃがみ 1.5m / 静止 0（検知なし）
        float noise = _playerStealth.FootstepNoiseRadius;
        if (noise > 0f && sqDist <= noise * noise) return true;

        // ④ ライト検知
        //    プレイヤーが懐中電灯をONにしていて、
        //    距離が _lightDetectRange (20m) 以内かつ
        //    プレイヤーの正面から敵への角度が _lightDetectAngle (60°) 以内なら検知。
        //    ＝懐中電灯を敵の方向に向けると遠くからでも気づかれる
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
