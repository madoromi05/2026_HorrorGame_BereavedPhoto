using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NavMeshAgent を使った敵AI
/// スタートルームをプレイヤーが脱出した時点で Activate() が呼ばれ、
/// 以降はプレイヤーを常時追跡しながら常にプレイヤー方向を向く
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("注視点")]
    [SerializeField] private Transform _aimPoint;

    [Header("追跡")]
    [SerializeField] private float _chaseSpeed = 4f;
    [SerializeField] private float _speedAfterAnalysis = 5.5f;          // 別種類の解析完了時に切り替える移動速度
    [SerializeField] private float _chaseDestUpdateInterval = 0.6f;     // 目的地再計算
    [SerializeField] private float _catchDistance = 3.0f;               // 捕まえる判定の距離

    private NavMeshAgent    _agent;
    private Transform       _player;
    private GameOverHandler _gameOverHandler;
    private float           _chaseDestTimer;
    private bool            _isActivated;
    private Coroutine       _stunCoroutine;		// 動いているスタン。null ならスタン中ではない

    private IChaseDestinationProvider _destinationProvider;	// 目的地の決め方を差し替える拡張点（HandEnemy の先回りなど）。無ければ現在位置を追う

    public bool IsActivated => _isActivated;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = false;
        _agent.speed = _chaseSpeed;
        TryGetComponent(out _destinationProvider);
    }

    private void OnEnable()  => EnemyRegistry.Register(this);
    private void OnDisable() => EnemyRegistry.Unregister(this);

    public void SetPlayer(Transform player)
    {
        if (player == null)
        {
            DebugCustom.LogWarning($"EnemyController.SetPlayer() に null が渡されました。", this);
            return;
        }

            _player = player;
        _gameOverHandler = _player.GetComponent<GameOverHandler>();
    }

    public void Activate()
    {
        _isActivated = true;
    }

    public void Stop()
    {
        _isActivated = false;
        if (_stunCoroutine != null)				// スタンが明けたときに追跡へ戻らないように止める
        {
            StopCoroutine(_stunCoroutine);
            _stunCoroutine = null;
        }
        if (!_agent.isOnNavMesh) return;        // 退場処理と競合しても安全に抜ける
        _agent.isStopped = true;
        _agent.velocity  = Vector3.zero;        // 残留速度による滑走を消し、その場で即停止
        _agent.ResetPath();                     // 追跡経路を破棄
    }

    /// <summary>
    /// この敵をシーンから退場させる。解析完了などの外部トリガーから呼ぶ
    /// 敵の退場は必ずここを通す（退場の仕方を変えるときの入口を 1 か所にするため）
    /// </summary>
    public void Despawn()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// 指定した種類（GhostType）の敵をすべて退場させる
    /// </summary>
    public static void DespawnByType(EnemyType type)
    {
        var targets = EnemyRegistry.OfType(type);
        foreach (var controller in targets)
            controller.Despawn();
        DebugCustom.Log($"[EnemyController] {type} の敵 {targets.Count} 体を退場させました。");
    }

    /// <summary>
    /// この敵の移動速度を、別種類の解析完了時用の速度（_speedAfterAnalysis）に切り替える
    /// </summary>
    public void ApplySpeedAfterAnalysis()
    {
        _chaseSpeed = _speedAfterAnalysis;
        if (_agent != null) _agent.speed = _chaseSpeed;
    }

    /// <summary>
    /// 指定した種類（GhostType）の敵すべての移動速度を、各自の _speedAfterAnalysis に切り替える
    /// 別種類の解析完了時に、残っている種類を加速させる用途で呼ぶ
    /// </summary>
    public static void ApplySpeedAfterAnalysisByType(EnemyType type)
    {
        var targets = EnemyRegistry.OfType(type);
        foreach (var controller in targets)
            controller.ApplySpeedAfterAnalysis();
        DebugCustom.Log($"[EnemyController] {type} の敵 {targets.Count} 体の速度を切り替えました。");
    }

    public void Stun(float duration)
    {
        if (!_isActivated) return;
        _stunCoroutine = StartCoroutine(StunCoroutine(duration));
    }

    private System.Collections.IEnumerator StunCoroutine(float duration)
    {
        _agent.isStopped = true;
        _isActivated = false;
        yield return new WaitForSeconds(duration);
        if (this == null || _agent == null) yield break;
        _agent.isStopped = false;
        _isActivated = true;
        _stunCoroutine = null;
    }

    private void Update()
    {
        if (_player == null) return;

        if (!_isActivated) return;

        // 捕捉判定：起動後のみ有効
        if (_gameOverHandler != null)
        {
            var dx = _player.position.x - transform.position.x;
            var dz = _player.position.z - transform.position.z;
            float distSq = dx * dx + dz * dz;
            if (distSq < _catchDistance * _catchDistance)
            {
                _gameOverHandler.TriggerGameOver(transform, _aimPoint);
                return;
            }
        }

        // 常にプレイヤー方向を向く（Y軸のみ）
        if (FlatLook.TryGetRotation(_player.position - transform.position, out var toPlayer))
            transform.rotation = toPlayer;

        // 移動先を定期更新
        _chaseDestTimer -= Time.deltaTime;
        if (_chaseDestTimer <= 0f)
        {
            Vector3 destination = _destinationProvider != null
                ? _destinationProvider.GetChaseDestination(_player)
                : _player.position;
            _agent.SetDestination(destination);
            _chaseDestTimer = _chaseDestUpdateInterval;
        }
    }
}
