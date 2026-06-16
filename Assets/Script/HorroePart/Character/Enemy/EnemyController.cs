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
    [Header("追跡")]
    [SerializeField] private float _chaseSpeed = 6f;
    [SerializeField] private float _chaseDestUpdateInterval = 0.6f;
    [SerializeField] private float _catchDistance = 1.2f;

    private NavMeshAgent    _agent;
    private Transform       _player;
    private GameOverHandler _gameOverHandler;
    private float           _chaseDestTimer;
    private bool            _isActivated;

    public bool IsActivated => _isActivated;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = false;
        _agent.speed = _chaseSpeed;
    }

    public void SetPlayer(Transform player)
    {
        _player = player;
        _gameOverHandler = _player.GetComponent<GameOverHandler>();
        DebugCustom.Log("EnemyController: Player set: " + player.name, this);
    }

    public void Activate() => _isActivated = true;

    private void Update()
    {
        if (_player == null) return;

        // 捕捉判定：距離に関わらず近距離なら即ゲームオーバー
        if (_gameOverHandler != null)
        {
            var dx = _player.position.x - transform.position.x;
            var dz = _player.position.z - transform.position.z;
            if (dx * dx + dz * dz < _catchDistance * _catchDistance)
            {
                _gameOverHandler.TriggerGameOver(transform);
                return;
            }
        }

        if (!_isActivated) return;

        // 常にプレイヤー方向を向く（Y軸のみ）
        Vector3 dir = _player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);

        // 移動先を定期更新
        _chaseDestTimer -= Time.deltaTime;
        if (_chaseDestTimer <= 0f)
        {
            _agent.SetDestination(_player.position);
            _chaseDestTimer = _chaseDestUpdateInterval;
        }
    }
}
