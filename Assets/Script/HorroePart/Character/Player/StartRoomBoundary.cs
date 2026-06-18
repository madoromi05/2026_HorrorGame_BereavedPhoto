using UnityEngine;

/// <summary>
/// プレイヤーがスタートルームを出た瞬間に全敵の追跡を開始させるトリガー。
/// ダンジョン生成後のプレイヤースポーン位置を基準に動的に判定するため、
/// 固定のトリガーコライダーなしで動作する。
/// HorrorScene 内の任意の GameObject にアタッチして使用する。
/// </summary>
public class StartRoomBoundary : MonoBehaviour
{
    [Tooltip("この距離を超えたらスタートルーム脱出と判定（メートル）")]
    [SerializeField] private float _exitRadius = 8f;

    private Transform _player;
    private Vector3   _startPos;
    private bool      _triggered;
    private float     _logTimer;

    private void Start()
    {
        var linker = FindObjectOfType<EnemyPlayerLinker>();
        if (linker == null || linker.PlayerTransform == null)
        {
            DebugCustom.LogError("[StartRoomBoundary] EnemyPlayerLinker からプレイヤーを取得できません。", this);
            enabled = false;
            return;
        }

        _player   = linker.PlayerTransform;
        _startPos = _player.position;
        DebugCustom.Log($"[StartRoomBoundary] 初期化完了 startPos={_startPos} exitRadius={_exitRadius}", this);
    }

    private void Update()
    {
        if (_triggered) return;

        var dx   = _player.position.x - _startPos.x;
        var dz   = _player.position.z - _startPos.z;
        var dist = Mathf.Sqrt(dx * dx + dz * dz);

        _logTimer -= Time.deltaTime;
        if (_logTimer <= 0f)
        {
            DebugCustom.Log($"[StartRoomBoundary] 現在距離={dist:F1} / 脱出半径={_exitRadius}", this);
            _logTimer = 3f;
        }

        if (dx * dx + dz * dz < _exitRadius * _exitRadius) return;

        _triggered = true;
        ActivateAllEnemies();
    }

    private void ActivateAllEnemies()
    {
        var enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        DebugCustom.Log($"[StartRoomBoundary] Activate 呼び出し。対象敵数={enemies.Length}", this);
        foreach (var e in enemies)
            e.Activate();

        AudioManager.Instance?.PlayBgm(BgmType.GameChase);
        enabled = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.3f);
        Gizmos.DrawSphere(Application.isPlaying ? _startPos : transform.position, _exitRadius);
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.8f);
        Gizmos.DrawWireSphere(Application.isPlaying ? _startPos : transform.position, _exitRadius);
    }
#endif
}
