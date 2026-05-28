using UnityEngine;

/// <summary>
/// 敵がプレイヤーを検知するロジックを EnemyController から分離したコンポーネント。
/// 検知パラメータ（視界・音・ライト）をここで一括管理する。
///
/// _sightLayerMask は SetPlayer() 時にプレイヤーと敵自身のレイヤーを除いた
/// 全レイヤーとして自動計算するため Inspector 設定不要。
/// </summary>
public class EnemyPerception : MonoBehaviour
{
    [Header("視界検知")]
    [SerializeField] private float _sightRange = 8f;
    [SerializeField] private float _sightHalfAngle = 35f;
    [SerializeField] private float _eyeHeightOffset = 1.5f;

    // _sightLayerMask は SetPlayer() 時に自動計算するため SerializeField にしない。
    // 「プレイヤーレイヤー ∪ 敵レイヤー」を除いた全レイヤー（= 壁・環境のみ）。
    private LayerMask _sightLayerMask;

    [Header("ライト検知")]
    [SerializeField] private float _lightDetectRange = 20f;
    [SerializeField] private float _lightDetectAngle = 60f;

    private Transform _player;
    private PlayerStealthStatus _playerStealth;

    /// <summary>
    /// EnemyController から呼ぶ。プレイヤー参照のセットと LayerMask の自動計算を行う。
    /// </summary>
    public void SetPlayer(Transform player)
    {
        _player = player;
        _playerStealth = player.GetComponent<PlayerStealthStatus>();

        if (_playerStealth == null)
            DebugCustom.LogWarning($"[EnemyPerception] PlayerStealthStatusが見つかりません: {player.name}");

        // プレイヤーレイヤーと自身（敵）レイヤーを除いた全レイヤー = 壁・環境のみ
        int playerLayer = player.gameObject.layer;
        int enemyLayer  = gameObject.layer;
        _sightLayerMask = ~((1 << playerLayer) | (1 << enemyLayer));
    }

    public bool IsPlayerDetected()
    {
        if (_player == null) return false;

        Vector3 diff = _player.position - transform.position;
        float sqDist = diff.sqrMagnitude;

        // 視界（FOVコーン + レイキャスト）
        if (sqDist <= _sightRange * _sightRange)
        {
            float angle = Vector3.Angle(transform.forward, diff.normalized);
            if (angle <= _sightHalfAngle)
            {
                Vector3 origin = transform.position + Vector3.up * _eyeHeightOffset;
                Vector3 target = _player.position   + Vector3.up * 1f;
                float dist = Vector3.Distance(origin, target);
                // Raycast が false = 遮蔽物なし = 視線通り = 検知
                if (!Physics.Raycast(origin, (target - origin).normalized, dist, _sightLayerMask))
                    return true;
            }
        }

        if (_playerStealth == null) return false;

        // 音検知（値は PlayerStealthStatus の Inspector で調整）
        float noise = _playerStealth.FootstepNoiseRadius;
        if (noise > 0f && sqDist <= noise * noise) return true;

        // ライト検知
        if (_playerStealth.IsLightOn && sqDist <= _lightDetectRange * _lightDetectRange)
        {
            Vector3 toEnemy = (transform.position - _player.position).normalized;
            float angle = Vector3.Angle(_player.forward, toEnemy);
            if (angle <= _lightDetectAngle) return true;
        }

        return false;
    }
}
