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

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            DebugCustom.LogWarning("[StartRoomBoundary] Player タグのオブジェクトが見つかりません。");
            enabled = false;
            return;
        }

        _player   = playerObj.transform;
        _startPos = _player.position;
    }

    private void Update()
    {
        if (_triggered) return;

        var dx = _player.position.x - _startPos.x;
        var dz = _player.position.z - _startPos.z;
        if (dx * dx + dz * dz < _exitRadius * _exitRadius) return;

        _triggered = true;
        ActivateAllEnemies();
    }

    private void ActivateAllEnemies()
    {
        foreach (var e in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
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
