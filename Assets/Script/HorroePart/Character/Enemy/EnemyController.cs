using UnityEngine;

/// <summary>
/// Player を追跡する敵の行動を管理するクラス。
/// 検知ロジックは EnemyPerception コンポーネントに委譲する（警戒度 AwarenessLevel）。
/// 徘徊ロジックは IEnemyBehavior（RoomWanderer / MapWanderer）に委譲する。
///
/// 検知の二値ではなく、警戒度に応じた多段ステートマシンで行動する。
///   Patrol     : 通常巡回（IEnemyBehavior.Tick）
///   Suspicious : 警戒。最後の刺激方向へ振り向きつつ最後に見た地点へ寄って確認
///   Chase      : 追跡（完全発見）
///   Search     : 見失い後、最後に見た地点周辺をステージ制でうろついて捜索
///   Feint      : 捜索を切り上げて立ち去る素振り。確率で振り返って再捜索（再来）
/// 巡回へ戻る瞬間に IEnemyBehavior.OnChaseEnded を呼び、Behavior 側に状態リセットを通知する。
///
/// 追跡/捜索衝突時の停止を防ぐために WallSlider で移動方向を壁面に合わせて補正する。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    public enum AIState { Patrol, Suspicious, Chase, Search, Feint }

    [Header("追跡")]
    [SerializeField] private float _chaseSpeed = 6f;
    [SerializeField] private float _accelerationForce = 30f;

    [Header("回転")]
    [SerializeField] private float _rotateSpeed = 10f;

    [Header("捜索/警戒")]
    // 警戒(Suspicious)・立ち去り(Feint)時の移動速度
    [SerializeField] private float _suspiciousSpeed = 1.5f;
    // 捜索(Search)時の移動速度
    [SerializeField] private float _searchSpeed = 3f;
    // 見失い後に捜索を続ける秒数
    [SerializeField] private float _searchDuration = 20f;
    // 最後に見た地点周辺をうろつく最大半径
    [SerializeField] private float _searchRoamRadius = 4f;
    // 次のうろつき先を選び直す間隔（秒）
    [SerializeField] private float _searchRepickInterval = 3f;
    // 捜索を切り上げて立ち去る素振りの秒数
    [SerializeField] private float _feintDuration = 4f;
    // 立ち去り後に振り返って再捜索する確率
    [Range(0f, 1f)]
    [SerializeField] private float _feintReturnChance = 0.5f;

    private Rigidbody _rb;
    private WallSlider _wallSlider;
    private EnemyPerception _detector;
    private Transform _player;
    private IEnemyBehavior _wanderBehavior;
    private GameOverHandler _gameOverHandler;

    private AIState _state = AIState.Patrol;

    // 捜索ランタイム
    private Vector3 _searchCenter;
    private Vector3 _searchPoint;
    private float _searchTimer;
    private float _searchRepickTimer;
    private float _currentRoamRadius;

    // フェイント（立ち去り）ランタイム
    private float _feintTimer;
    private Vector3 _feintDirection;
    private bool _feintWillReturn;

    private const float kArrivalRadius = 1f;

    /// <summary>現在のAI状態（デバッグ/将来のHUD用）。</summary>
    public AIState State => _state;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _wallSlider = GetComponent<WallSlider>();

        // EnemyPerception が Prefab になくても Instantiate 時に自動追加する
        _detector = GetComponent<EnemyPerception>() ?? gameObject.AddComponent<EnemyPerception>();

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
        _detector?.SetPlayer(player);  // 検知コンポーネントにも同時に渡す

        if (_gameOverHandler == null)
            DebugCustom.LogWarning($"[EnemyController] GameOverHandlerが見つかりません: {_player.name}");
    }

    private void FixedUpdate()
    {
        if (_player == null) return;

        if (_detector == null)
        {
            _wanderBehavior?.Tick();
            return;
        }

        _detector.UpdateAwareness(Time.fixedDeltaTime);
        float awareness = _detector.AwarenessLevel;

        switch (_state)
        {
            case AIState.Patrol:     TickPatrol(awareness);     break;
            case AIState.Suspicious: TickSuspicious(awareness); break;
            case AIState.Chase:      TickChase(awareness);      break;
            case AIState.Search:     TickSearch(awareness);     break;
            case AIState.Feint:      TickFeint(awareness);      break;
        }
    }

    // ---------------- 各状態の処理 ----------------

    private void TickPatrol(float awareness)
    {
        if (awareness >= 1f) { _state = AIState.Chase; Chase(); return; }
        if (awareness >= _detector.SuspicionThreshold) { _state = AIState.Suspicious; return; }
        _wanderBehavior?.Tick();
    }

    /// <summary>
    /// 警戒状態。最後の刺激方向へ振り向きながら、最後に見た地点へゆっくり寄って確認する。
    /// 警戒度が満タンになれば追跡、閾値を下回れば巡回へ戻る。
    /// </summary>
    private void TickSuspicious(float awareness)
    {
        if (awareness >= 1f) { _state = AIState.Chase; Chase(); return; }
        if (awareness < _detector.SuspicionThreshold) { EndToPatrol(); return; }

        Vector3 target = _detector.HasLastKnown
            ? _detector.LastKnownPosition
            : transform.position + _detector.LastStimulusDirection;

        Vector3 dir = Flatten(target - transform.position);
        if (dir.magnitude > kArrivalRadius)
            ApplyMovement(dir.normalized, _suspiciousSpeed);
        else
            RotateToward(_detector.LastStimulusDirection);  // 到達済みならその場で振り向いて探す
    }

    private void TickChase(float awareness)
    {
        if (awareness < 1f) { BeginSearch(); return; }
        Chase();
    }

    /// <summary>
    /// 見失い後の捜索。Stage1=最後に見た地点へ直行、Stage2=その周辺を半径を広げながらうろつく。
    /// 時間切れで Feint へ。途中で再発見すれば追跡へ復帰。
    /// </summary>
    private void TickSearch(float awareness)
    {
        if (awareness >= 1f) { _state = AIState.Chase; Chase(); return; }

        _searchTimer -= Time.fixedDeltaTime;
        if (_searchTimer <= 0f) { BeginFeint(); return; }

        _searchRepickTimer -= Time.fixedDeltaTime;

        Vector3 dir = Flatten(_searchPoint - transform.position);
        if (dir.magnitude <= kArrivalRadius || _searchRepickTimer <= 0f)
        {
            PickSearchPoint();
            dir = Flatten(_searchPoint - transform.position);
        }

        ApplyMovement(dir.normalized, _searchSpeed);
    }

    /// <summary>
    /// 捜索を切り上げて立ち去る素振り。確率で振り返り、もう一度短い捜索へ戻る（再来）。
    /// </summary>
    private void TickFeint(float awareness)
    {
        if (awareness >= 1f) { _state = AIState.Chase; Chase(); return; }

        _feintTimer -= Time.fixedDeltaTime;
        if (_feintTimer <= 0f)
        {
            if (_feintWillReturn)
            {
                _feintWillReturn = false;            // 再来は一度だけ
                _searchTimer = _searchDuration * 0.5f;
                _searchRepickTimer = 0f;
                _currentRoamRadius = _searchRoamRadius * 0.5f;
                _searchPoint = _searchCenter;
                _state = AIState.Search;
                return;
            }
            EndToPatrol();
            return;
        }

        ApplyMovement(_feintDirection, _suspiciousSpeed);
    }

    // ---------------- 状態遷移ヘルパー ----------------

    private void BeginSearch()
    {
        _searchCenter = _detector.HasLastKnown ? _detector.LastKnownPosition : transform.position;
        _searchTimer = _searchDuration;
        _searchRepickTimer = 0f;
        _currentRoamRadius = _searchRoamRadius * 0.5f;  // 最初は狭く、徐々に拡大
        _searchPoint = _searchCenter;
        _state = AIState.Search;
    }

    private void PickSearchPoint()
    {
        // うろつく半径を段階的に拡大して隣接探索を表現する
        _currentRoamRadius = Mathf.Min(_currentRoamRadius + 0.5f, _searchRoamRadius);
        Vector2 r = Random.insideUnitCircle * _currentRoamRadius;
        _searchPoint = new Vector3(_searchCenter.x + r.x, transform.position.y, _searchCenter.z + r.y);
        _searchRepickTimer = _searchRepickInterval;
    }

    private void BeginFeint()
    {
        _feintTimer = _feintDuration;
        _feintWillReturn = Random.value < _feintReturnChance;

        Vector3 away = Flatten(transform.position - _searchCenter);
        _feintDirection = away.sqrMagnitude > 0.01f ? away.normalized : transform.forward;
        _state = AIState.Feint;
    }

    private void EndToPatrol()
    {
        _wanderBehavior?.OnChaseEnded();
        _state = AIState.Patrol;
    }

    // ---------------- 移動 ----------------

    private void Chase()
    {
        var direction = Flatten(_player.position - transform.position).normalized;
        ApplyMovement(direction, _chaseSpeed);
    }

    /// <summary>
    /// WallSlider で壁面に合わせて補正してから AddForce で移動する。
    /// velocity の直接操作は AddForce の結果と壁の反力が干渉するため一切行わない。
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

        // 目標速度を超えている方向には Force を加えない（velocity を直接触らずに過速を防ぐ）
        if (Vector3.Dot(diff, slideDir) > 0f)
            _rb.AddForce(diff * _accelerationForce, ForceMode.Force);
    }

    /// <summary>その場で指定方向（XZ）へ向き直る。移動はしない。</summary>
    private void RotateToward(Vector3 direction)
    {
        var flat = Flatten(direction);
        if (flat.sqrMagnitude < 0.0001f) return;
        var targetRot = Quaternion.LookRotation(flat.normalized);
        _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRot, _rotateSpeed * Time.fixedDeltaTime);
    }

    private static Vector3 Flatten(Vector3 v) => new Vector3(v.x, 0f, v.z);

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject != _player.gameObject) return;
        _gameOverHandler?.TriggerGameOver();
    }
}
