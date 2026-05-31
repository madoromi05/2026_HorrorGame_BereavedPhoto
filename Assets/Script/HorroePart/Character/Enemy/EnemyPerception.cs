using UnityEngine;

/// <summary>
/// 敵がプレイヤーを検知するロジックを EnemyController から分離したコンポーネント。
/// 検知パラメータ（視界・音・ライト）をここで一括管理する。
///
/// 検知は二値ではなく「警戒度（AwarenessLevel 0〜1）」で段階的に表現する。
/// EnemyController が毎フレーム UpdateAwareness() を呼び、刺激の強さに応じて
/// 警戒度を蓄積・減衰させる。蓄積レートは以下で変化する。
///   ・距離   : 近いほど速い
///   ・視野角 : 正面（中心視野）ほど速く、周辺視野は遅い
///   ・明るさ : プレイヤーがライト点灯中は速く、かつ視界レンジが延びる（近似）
/// 音検知・ライトビーム検知は一定の蓄積ボーナスとして加算する。
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

    [Header("警戒度")]
    // 正面・至近で視認したときの1秒あたり蓄積量（基準値）
    [SerializeField] private float _baseGainRate = 1.2f;
    // プレイヤーがライト点灯中の蓄積倍率＆実効視界レンジ倍率
    [SerializeField] private float _lightGainMultiplier = 1.6f;
    // 音検知時に加算する1秒あたり蓄積量
    [SerializeField] private float _soundGainBonus = 0.6f;
    // ライトビーム検知時に加算する1秒あたり蓄積量
    [SerializeField] private float _lightBeamGainBonus = 0.6f;
    // 刺激が無いときの1秒あたり減衰量
    [SerializeField] private float _awarenessDecayRate = 0.35f;
    // この警戒度以上で「警戒(Suspicious)」状態に入る。1.0で追跡(Chase)
    [Range(0f, 1f)]
    [SerializeField] private float _suspicionThreshold = 0.4f;

    private Transform _player;
    private PlayerStealthStatus _playerStealth;

    /// <summary>0〜1 の警戒度。1 で完全に発見（追跡）。</summary>
    public float AwarenessLevel { get; private set; }

    /// <summary>この値以上で警戒状態に入る閾値。</summary>
    public float SuspicionThreshold => _suspicionThreshold;

    /// <summary>最後にプレイヤーを認識した位置（捜索の手掛かり）。</summary>
    public Vector3 LastKnownPosition { get; private set; }

    /// <summary>一度でも認識したことがあるか。</summary>
    public bool HasLastKnown { get; private set; }

    /// <summary>最後に刺激を受けた方向（XZ平面・振り向き先）。</summary>
    public Vector3 LastStimulusDirection { get; private set; } = Vector3.forward;

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

    /// <summary>
    /// 毎フレーム（FixedUpdate）EnemyController から呼ぶ。
    /// 刺激の強さに応じて AwarenessLevel を増減させる。
    /// </summary>
    public void UpdateAwareness(float dt)
    {
        if (_player == null) return;

        float gain = 0f;
        bool stimulus = false;

        Vector3 diff = _player.position - transform.position;
        float sqDist = diff.sqrMagnitude;
        Vector3 flatDir = new Vector3(diff.x, 0f, diff.z);

        bool lightOn = _playerStealth != null && _playerStealth.IsLightOn;

        // ライト点灯中は視界レンジが延びる（近似）
        float effectiveRange = lightOn ? _sightRange * _lightGainMultiplier : _sightRange;

        // 視界（FOVコーン + レイキャスト）
        if (sqDist <= effectiveRange * effectiveRange)
        {
            float dist = Mathf.Sqrt(sqDist);
            float angle = Vector3.Angle(transform.forward, diff.normalized);
            if (angle <= _sightHalfAngle)
            {
                Vector3 origin = transform.position + Vector3.up * _eyeHeightOffset;
                Vector3 target = _player.position   + Vector3.up * 1f;
                float rayDist = Vector3.Distance(origin, target);
                // Raycast が false = 遮蔽物なし = 視線通り = 視認成功
                if (!Physics.Raycast(origin, (target - origin).normalized, rayDist, _sightLayerMask))
                {
                    float distFactor  = Mathf.Clamp01(1f - dist / effectiveRange);          // 近いほど大
                    float angleFactor = Mathf.Clamp01(1f - angle / _sightHalfAngle);        // 正面ほど大
                    // 周辺視野でも 0 にはせず、中心視野ほど速くなるよう 0.3〜1.0 で補間
                    float rate = _baseGainRate * distFactor * (0.3f + 0.7f * angleFactor);
                    if (lightOn) rate *= _lightGainMultiplier;

                    gain += rate;
                    stimulus = true;
                    RecordStimulus(flatDir);
                }
            }
        }

        if (_playerStealth != null)
        {
            // 音検知（足音半径は PlayerStealthStatus が移動状態ごとに更新）
            float noise = _playerStealth.FootstepNoiseRadius;
            if (noise > 0f && sqDist <= noise * noise)
            {
                gain += _soundGainBonus;
                stimulus = true;
                RecordStimulus(flatDir);
            }

            // ライトビーム検知（プレイヤーが敵の方向へライトを向けている）
            if (lightOn && sqDist <= _lightDetectRange * _lightDetectRange)
            {
                Vector3 toEnemy = (transform.position - _player.position).normalized;
                float beamAngle = Vector3.Angle(_player.forward, toEnemy);
                if (beamAngle <= _lightDetectAngle)
                {
                    gain += _lightBeamGainBonus;
                    stimulus = true;
                    RecordStimulus(flatDir);
                }
            }
        }

        if (stimulus)
            AwarenessLevel += gain * dt;
        else
            AwarenessLevel -= _awarenessDecayRate * dt;

        AwarenessLevel = Mathf.Clamp01(AwarenessLevel);
    }

    /// <summary>刺激を受けた際に最後の認識位置・方向を更新する。</summary>
    private void RecordStimulus(Vector3 flatDir)
    {
        LastKnownPosition = _player.position;
        HasLastKnown = true;
        if (flatDir.sqrMagnitude > 0.0001f)
            LastStimulusDirection = flatDir;
    }
}
