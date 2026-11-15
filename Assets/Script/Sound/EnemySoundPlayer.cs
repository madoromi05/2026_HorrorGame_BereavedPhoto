using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 敵に取り付けて周期的に SE を 3D 音源で再生し、Raycast による壁オクルージョンを適用する
/// AudioSource と AudioLowPassFilter を RequireComponent で自動追加するため、
/// 既存の Prefab にこのコンポーネントを追加するだけで動作する
///
/// オクルージョン：
///   壁による減衰の見積もりと適用は SoundOcclusion が持つ。ここは Inspector の値で作り、毎フレーム呼ぶだけ
/// </summary>
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioLowPassFilter))]
public class EnemySoundPlayer : MonoBehaviour
{
    [Header("SE 間隔")]
    [SerializeField] private AudioMixerGroup _outputGroup;
    [SerializeField] private float _seInterval         = 3f;
    [SerializeField] private float _seIntervalVariance = 2f;

    [Header("3D 音源")]
    [SerializeField] private float _minDistance = 1f;
    [SerializeField] private float _maxDistance = 35f;

    [Header("オクルージョン")]
    [Tooltip("壁として扱うレイヤーを設定。未設定（0）の場合はオクルージョンなし。")]
    [SerializeField] private LayerMask _occlusionLayer;
    [SerializeField] private float _maxAttenuationDb     = 20f;
    [SerializeField] private float _dbPerMeter           = 3f;
    [SerializeField] private float _maxDbChangePerSecond = 10f;
    [SerializeField] private float _openCutoffHz         = 22000f;
    [SerializeField] private float _occludedCutoffHz     = 800f;

    private AudioSource    _audioSource;
    private SoundOcclusion _occlusion;
    private Transform      _listenerTransform;
    private SeType         _seType;

    private float _seTimer;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        var lowPassFilter = GetComponent<AudioLowPassFilter>();

        _audioSource.spatialBlend = 1f;
        _audioSource.rolloffMode  = AudioRolloffMode.Linear;
        _audioSource.minDistance  = _minDistance;
        _audioSource.maxDistance  = _maxDistance;
        _audioSource.playOnAwake  = false;
        _audioSource.volume       = 1f;

        if (_outputGroup != null)
            _audioSource.outputAudioMixerGroup = _outputGroup;

        _occlusion = new SoundOcclusion(transform, _audioSource, lowPassFilter, _openCutoffHz);

        var identity = GetComponent<GhostIdentity>();
        _seType = identity != null && identity.GhostType == EnemyType.SmileEnemies
            ? SeType.EnemyFather
            : SeType.EnemyMother;

        _seTimer = Random.Range(0f, _seInterval);	// 複数の敵の SE が同時に鳴らないようランダムオフセットを付ける
    }

    private void Start()
    {
        var listener = FindFirstObjectByType<AudioListener>();
        if (listener != null)
            _listenerTransform = listener.transform;
    }

    private void Update()
    {
        // ゲームオーバー確定後は敵の SE を止める（AudioManager を経由しない音源のため個別に抑止する）
        if (AudioManager.Instance != null && AudioManager.Instance.IsSeSuppressed)
        {
            if (_audioSource.isPlaying) _audioSource.Stop();
            return;
        }

        _occlusion.Tick(_listenerTransform, Time.deltaTime, OcclusionSettings());

        _seTimer -= Time.deltaTime;
        if (_seTimer <= 0f)
        {
            TryPlaySe();
            _seTimer = _seInterval + Random.Range(-_seIntervalVariance, _seIntervalVariance);
        }
    }

    private SoundOcclusion.Settings OcclusionSettings() => new SoundOcclusion.Settings(	// Inspector の値を毎回読み直す
        _occlusionLayer, _maxAttenuationDb, _dbPerMeter, _maxDbChangePerSecond, _openCutoffHz, _occludedCutoffHz);

    private void TryPlaySe()
    {
        var resource = AudioManager.Instance?.GetSeResource(_seType);
        if (resource != null)
        {
            _audioSource.resource = resource;
            _audioSource.Play();
        }
    }
}
