using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// EnemyController に対して「どこを追跡目的地にするか」を差し込むための拡張点。
/// このコンポーネントが敵ルートに存在する場合、EnemyController は
/// _player.position の代わりにここで計算した目的地を使う。
/// </summary>
public interface IChaseDestinationProvider
{
    // 回の追跡で NavMeshAgent に渡す目的地を返す
    Vector3 GetChaseDestination(Transform player);
}

/// <summary>
/// HandEnemy 用の「先回り（迎撃）」追跡ロジック。
/// プレイヤーの現在位置ではなく、進行方向を予測した少し先の地点を目的地にすることで、
/// 全ての敵が同一点へ収束して団子化するのを避けつつ、包囲されるような圧を演出する。
///
/// 使い方:
///   EnemyController と同じ GameObject（敵ルート）にアタッチする。
///   GhostType が HandEnemy のときのみ先回りが有効になり、それ以外では通常追跡にフォールバックする。
/// </summary>
public class EnemyInterceptChaser : MonoBehaviour, IChaseDestinationProvider
{
    [Header("先回り")]
    [Tooltip("プレイヤーの進行方向へ何秒先を狙うか。")]
    [SerializeField] private float _leadTime = 1.2f;

    [Tooltip("先読みで前に出せる最大距離。予測が暴れて遠くへ飛ぶのを防ぐ。")]
    [SerializeField] private float _maxLeadDistance = 6f;

    [Tooltip("プレイヤー速度の平滑化の速さ。大きいほど機敏（急な方向転換に敏感）。")]
    [SerializeField] private float _velocityResponse = 6f;

    [Tooltip("予測地点を NavMesh 上へスナップする際の探索半径。")]
    [SerializeField] private float _navSampleRadius = 2f;

    private bool      _activeForType;
    private Transform _player;
    private Vector3   _lastPlayerPos;
    private Vector3   _smoothedVelocity;
    private bool      _hasLastPos;

    private void Awake()
    {
        // GhostIdentity は子にぶら下がっている場合があるので子まで探索する。
        var identity = GetComponent<GhostIdentity>();
        _activeForType = identity != null && identity.GhostType == EnemyType.HandEnemy;
    }

    private void Update()
    {
        // プレイヤーの速度をフレーム単位で追跡し、平滑化しておく。
        if (_player == null) return;

        Vector3 pos = _player.position;
        if (_hasLastPos && Time.deltaTime > 0f)
        {
            Vector3 instant = (pos - _lastPlayerPos) / Time.deltaTime;
            instant.y = 0f;
            // フレームレート非依存の指数平滑化。
            float t = 1f - Mathf.Exp(-_velocityResponse * Time.deltaTime);
            _smoothedVelocity = Vector3.Lerp(_smoothedVelocity, instant, t);
        }
        _lastPlayerPos = pos;
        _hasLastPos = true;
    }

    public Vector3 GetChaseDestination(Transform player)
    {
        // Update で速度を追えるようにプレイヤー参照をキャッシュしておく。
        _player = player;
        if (player == null) return transform.position;

        // HandEnemy 以外は先回りせず、従来通り現在位置へ向かう。
        if (!_activeForType) return player.position;

        Vector3 lead = _smoothedVelocity * _leadTime;
        if (lead.magnitude > _maxLeadDistance)
            lead = lead.normalized * _maxLeadDistance;

        Vector3 predicted = player.position + lead;

        // 予測地点が NavMesh 外なら現在位置へフォールバック。
        if (NavMesh.SamplePosition(predicted, out var hit, _navSampleRadius, NavMesh.AllAreas))
            return hit.position;

        return player.position;
    }
}
