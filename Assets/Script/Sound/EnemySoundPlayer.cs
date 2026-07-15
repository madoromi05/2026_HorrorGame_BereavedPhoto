using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 敵に取り付けて周期的に SE を 3D 音源で再生し、Raycast による壁オクルージョンを適用する。
/// AudioSource と AudioLowPassFilter を RequireComponent で自動追加するため、
/// 既存の Prefab にこのコンポーネントを追加するだけで動作する。
///
/// オクルージョン：
///   敵 → AudioListener 間に壁（_occlusionLayer）が存在すれば
///   BoxCollider の厚さから dB 減衰量を推定し、ローパスフィルタも合わせて適用する。
///   変化は _maxDbChangePerSecond でなめらかに追従するため音がブツ切れにならない。
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

    private AudioSource        _audioSource;
    private AudioLowPassFilter _lowPassFilter;
    private Transform          _listenerTransform;
    private SeType             _seType;

    private float _seTimer;
    private float _currentAttenuationDb;
    private float _targetAttenuationDb;

    private void Awake()
    {
        _audioSource   = GetComponent<AudioSource>();
        _lowPassFilter = GetComponent<AudioLowPassFilter>();

        _audioSource.spatialBlend = 1f;
        _audioSource.rolloffMode  = AudioRolloffMode.Linear;
        _audioSource.minDistance  = _minDistance;
        _audioSource.maxDistance  = _maxDistance;
        _audioSource.playOnAwake  = false;
        _audioSource.volume       = 1f;

        if (_outputGroup != null)
            _audioSource.outputAudioMixerGroup = _outputGroup;

        _lowPassFilter.cutoffFrequency = _openCutoffHz;

        var identity = GetComponent<GhostIdentity>();
        _seType = identity != null && identity.GhostType == EnemyType.SmileEnemies
            ? SeType.EnemyFather
            : SeType.EnemyMother;

        // 複数の敵の SE が同時に鳴らないようランダムオフセットを付ける
        _seTimer = Random.Range(0f, _seInterval);
    }

    private void Start()
    {
        var listener = FindFirstObjectByType<AudioListener>();
        if (listener != null)
            _listenerTransform = listener.transform;
    }

    private void Update()
    {
        UpdateOcclusion();

        _seTimer -= Time.deltaTime;
        if (_seTimer <= 0f)
        {
            TryPlaySe();
            _seTimer = _seInterval + Random.Range(-_seIntervalVariance, _seIntervalVariance);
        }
    }

    private void TryPlaySe()
    {
        var resource = AudioManager.Instance?.GetSeResource(_seType);
        if (resource != null)
        {
            _audioSource.resource = resource;
            _audioSource.Play();
        }
    }

    private void UpdateOcclusion()
    {
        if (_listenerTransform == null || _occlusionLayer == 0) return;

        var toListener = _listenerTransform.position - transform.position;
        float dist = toListener.magnitude;

        _targetAttenuationDb = 0f;

        if (dist > 0.1f && Physics.Raycast(transform.position, toListener / dist, out var hit, dist, _occlusionLayer))
        {
            if (hit.collider is BoxCollider box)
            {
                var ws = Vector3.Scale(box.size, box.transform.lossyScale);
                float thickness = Mathf.Min(Mathf.Abs(ws.x), Mathf.Min(Mathf.Abs(ws.y), Mathf.Abs(ws.z)));
                _targetAttenuationDb = Mathf.Min(thickness * _dbPerMeter, _maxAttenuationDb);
            }
            else
            {
                _targetAttenuationDb = _maxAttenuationDb * 0.5f;
            }
        }

        float maxChange = _maxDbChangePerSecond * Time.deltaTime;
        _currentAttenuationDb += Mathf.Clamp(
            _targetAttenuationDb - _currentAttenuationDb,
            -maxChange, maxChange);

        _audioSource.volume = Mathf.Pow(10f, -_currentAttenuationDb / 20f);
        _lowPassFilter.cutoffFrequency = Mathf.Lerp(
            _openCutoffHz, _occludedCutoffHz,
            _currentAttenuationDb / Mathf.Max(_maxAttenuationDb, 0.001f));
    }
}
