using UnityEngine;

/// <summary>
/// 音源と聞き手の間にある壁で、音をこもらせる係
/// 敵 → AudioListener 間に壁（OcclusionLayer）が存在すれば
/// BoxCollider の厚さから dB 減衰量を推定し、ローパスフィルタも合わせて適用する
/// 変化は MaxDbChangePerSecond でなめらかに追従するため音がブツ切れにならない
/// EnemySoundPlayer が Awake で作り、毎フレーム Inspector の値を Settings にまとめて Tick へ渡す
/// 調整値は持たず、今の減衰量と部品の参照だけを持つ。音を鳴らす・止めるはしない
/// </summary>
public class SoundOcclusion
{
    /// <summary>
    /// 遮蔽の調整値。持ち主の Inspector の値から呼ぶたびに作る
    /// </summary>
    public readonly struct Settings
    {
        public readonly LayerMask OcclusionLayer;
        public readonly float     MaxAttenuationDb;
        public readonly float     DbPerMeter;
        public readonly float     MaxDbChangePerSecond;
        public readonly float     OpenCutoffHz;
        public readonly float     OccludedCutoffHz;

        public Settings(LayerMask occlusionLayer, float maxAttenuationDb, float dbPerMeter, float maxDbChangePerSecond,
                        float openCutoffHz, float occludedCutoffHz)
        {
            OcclusionLayer       = occlusionLayer;
            MaxAttenuationDb     = maxAttenuationDb;
            DbPerMeter           = dbPerMeter;
            MaxDbChangePerSecond = maxDbChangePerSecond;
            OpenCutoffHz         = openCutoffHz;
            OccludedCutoffHz     = occludedCutoffHz;
        }
    }

    private const float kMinRaycastDistance     = 0.1f;	// これより近いと向きが定まらないので、遮蔽を調べない
    private const float kNonBoxAttenuationRatio = 0.5f;	// 箱以外の壁は厚さが分からないので、最大の半分だけ下げる
    private const float kMinDivisor             = 0.001f;	// 最大減衰が 0 のときの 0 除算を防ぐ

    private readonly Transform          _emitter;
    private readonly AudioSource        _audioSource;
    private readonly AudioLowPassFilter _lowPassFilter;

    private float _currentAttenuationDb;

    public SoundOcclusion(Transform emitter, AudioSource audioSource, AudioLowPassFilter lowPassFilter, float openCutoffHz)
    {
        _emitter       = emitter;
        _audioSource   = audioSource;
        _lowPassFilter = lowPassFilter;

        _lowPassFilter.cutoffFrequency = openCutoffHz;	// ローパスを開いた状態から始める
    }

    public void Tick(Transform listener, float deltaTime, in Settings settings)
    {
        if (listener == null || settings.OcclusionLayer == 0) return;

        float targetDb  = EstimateOcclusionDb(listener, settings);
        float maxChange = settings.MaxDbChangePerSecond * deltaTime;
        _currentAttenuationDb += Mathf.Clamp(targetDb - _currentAttenuationDb, -maxChange, maxChange);

        ApplyAttenuation(settings);
    }

    private float EstimateOcclusionDb(Transform listener, in Settings settings)	// 聞き手との間にある壁から、下げる dB を見積もる。壁が無ければ 0
    {
        var toListener = listener.position - _emitter.position;
        float dist = toListener.magnitude;
        if (dist <= kMinRaycastDistance) return 0f;
        if (!Physics.Raycast(_emitter.position, toListener / dist, out var hit, dist, settings.OcclusionLayer)) return 0f;

        if (hit.collider is BoxCollider box)
            return Mathf.Min(ThinnestSide(box) * settings.DbPerMeter, settings.MaxAttenuationDb);
        return settings.MaxAttenuationDb * kNonBoxAttenuationRatio;
    }

    private static float ThinnestSide(BoxCollider box)	// 壁の厚さとみなす、いちばん薄い辺の長さ（ワールド）
    {
        var ws = Vector3.Scale(box.size, box.transform.lossyScale);
        return Mathf.Min(Mathf.Abs(ws.x), Mathf.Min(Mathf.Abs(ws.y), Mathf.Abs(ws.z)));
    }

    private void ApplyAttenuation(in Settings settings)	// 今の減衰を、音量とローパスフィルタに映す
    {
        _audioSource.volume = AudioDb.DbToLinear(-_currentAttenuationDb);
        _lowPassFilter.cutoffFrequency = Mathf.Lerp(
            settings.OpenCutoffHz, settings.OccludedCutoffHz,
            _currentAttenuationDb / Mathf.Max(settings.MaxAttenuationDb, kMinDivisor));
    }
}
