using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NavMeshAgent を使った敵AI。
/// スタートルームをプレイヤーが脱出した時点で Activate() が呼ばれ、
/// 以降はプレイヤーを常時追跡しながら常にプレイヤー方向を向く。
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("注視点")]
    [SerializeField] private Transform _aimPoint;

    [Header("追跡")]
    [SerializeField] private float _chaseSpeed = 4f;
    [SerializeField] private float _chaseDestUpdateInterval = 0.6f;     // 目的地再計算
    [SerializeField] private float _catchDistance = 3.0f;               // 捕まえる判定の距離

    private NavMeshAgent    _agent;
    private Transform       _player;
    private GameOverHandler _gameOverHandler;
    private float           _chaseDestTimer;
    private bool            _isActivated;

    // 目的地の決め方を差し替える拡張点（HandEnemy の先回りなど）。無ければ現在位置を追う。
    private IChaseDestinationProvider _destinationProvider;

    public bool IsActivated => _isActivated;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = false;
        _agent.speed = _chaseSpeed;
        TryGetComponent(out _destinationProvider);
    }

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
        StopCoroutine(nameof(StunCoroutine));   // スタン中のコルーチンがあれば停止
        _agent.isStopped = true;
    }

    /// <summary>
    /// この敵をシーンから退場させる。解析完了などの外部トリガーから呼ぶ。
    /// 今後フェードや消滅演出を足す場合はここに集約する。
    /// </summary>
    public void Despawn()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// 指定した種類（GhostType）の敵をすべて退場させる。
    /// GhostIdentity が子にあっても、EnemyController のルートごと消す。
    /// </summary>
    public static void DespawnByType(EnemyType type)
    {
        int removed = 0;
        foreach (var ghost in FindObjectsByType<GhostIdentity>(FindObjectsSortMode.None))
        {
            if (ghost.GhostType != type) continue;

            var controller = ghost.GetComponentInParent<EnemyController>();
            if (controller != null) controller.Despawn();
            else                    Destroy(ghost.gameObject);
            removed++;
        }
        DebugCustom.Log($"[EnemyController] {type} の敵 {removed} 体を退場させました。");
    }

    public void Stun(float duration)
    {
        if (!_isActivated) return;
        StartCoroutine(StunCoroutine(duration));
    }

    private System.Collections.IEnumerator StunCoroutine(float duration)
    {
        _agent.isStopped = true;
        _isActivated = false;
        yield return new WaitForSeconds(duration);
        if (this == null || _agent == null) yield break;
        _agent.isStopped = false;
        _isActivated = true;
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
        Vector3 dir = _player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);

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
