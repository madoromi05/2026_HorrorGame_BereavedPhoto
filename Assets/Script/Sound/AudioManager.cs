using UnityEngine;

/// <summary>
/// サウンド管理のエントリポイント（MonoBehaviour Singleton / DontDestroyOnLoad）。
/// Resources/AudioManager プレハブを RuntimeInitializeOnLoadMethod で自動生成する。
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("サブシステム")]
    [SerializeField] private BgmPlayer       _bgmPlayer;
    [SerializeField] private SePlayer        _sePlayer;
    [SerializeField] private AudioMixerProxy _mixerProxy;

    [Header("BGM クリップ")]
    [SerializeField] private AudioClip _bgmTitle;
    [SerializeField] private AudioClip _bgmGameNormal;
    [SerializeField] private AudioClip _bgmGameChase;
    [SerializeField] private AudioClip _bgmEnding;
    [SerializeField] private AudioClip _bgmScenario;
    [SerializeField] private AudioClip _seFootStep;

    [Header("SE クリップ")]
    [SerializeField] private AudioClip _seEnemyMother;
    [SerializeField] private AudioClip _seEnemyFather;
    [SerializeField] private AudioClip _seItemPickup;
    [SerializeField] private AudioClip _seAnalysisComplete;
    [SerializeField] private AudioClip _seStartRoomExit;
    [SerializeField] private AudioClip _seGameOver;

    // ---- 自動生成 ----
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance != null) return;
        var prefab = Resources.Load<AudioManager>("AudioManager");
        if (prefab != null)
        {
            Instantiate(prefab);
        }
        else
        {
            DebugCustom.LogWarning(
                "[AudioManager] Resources/AudioManager プレハブが見つかりません。" +
                "手動でシーンに配置するか Resources フォルダにプレハブを置いてください。");
        }
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---- BGM ----（複数同時不可）
    /// BGM をクロスフェードで切り替える。同じ曲が再生中の場合は何もしない。
    public void PlayBgm(BgmType type, float crossFadeDuration = 0.8f)
    {
        if (type == BgmType.None) { StopBgm(); return; }
        var clip = GetBgmClip(type);
        if (clip == null)
        {
            DebugCustom.LogWarning($"[AudioManager] BGM クリップ未設定: {type}");
            return;
        }
        _bgmPlayer.CrossFade(clip, crossFadeDuration);
    }

    /// BGM をフェードアウトして停止する。
    public void StopBgm(float fadeOut = 0.8f) => _bgmPlayer.Stop(fadeOut);

    // ---- SE ----
    /// SE を再生する（複数同時可）。
    public void PlaySe(SeType type, float volume = 1f, float pitch = 1f)
    {
        if(type == SeType.None) return;
        var clip = GetSeClip(type);
        if (clip == null)
        {
            DebugCustom.LogWarning($"[AudioManager] SE クリップ未設定: {type}");
            return;
        }
        _sePlayer.Play(clip, volume, pitch);
    }

    // ---- ボリューム / ミュート ----

    public void SetBgmVolume(float normalized) => _mixerProxy?.SetBgmVolume(normalized);
    public void SetSeVolume(float normalized)  => _mixerProxy?.SetSeVolume(normalized);
    public void MuteBgm(bool mute)             => _mixerProxy?.MuteBgm(mute);
    public void MuteSe(bool mute)              => _mixerProxy?.MuteSe(mute);

    public float BgmVolume => _mixerProxy != null ? _mixerProxy.GetBgmVolume() : 1f;
    public float SeVolume  => _mixerProxy != null ? _mixerProxy.GetSeVolume()  : 1f;

    // ---- クリップ取得 ----

    private AudioClip GetBgmClip(BgmType type) => type switch
    {
        BgmType.Title      => _bgmTitle,
        BgmType.GameNormal => _bgmGameNormal,
        BgmType.GameChase  => _bgmGameChase,
        BgmType.Ending     => _bgmEnding,
        BgmType.Scenario   => _bgmScenario,
        _                  => null,
    };

    
    public AudioClip GetSeClip(SeType type) => type switch
    {
        SeType.EnemyMother      => _seEnemyMother,
        SeType.EnemyFather      => _seEnemyFather,
        SeType.ItemPickup       => _seItemPickup,
        SeType.AnalysisComplete => _seAnalysisComplete,
        SeType.FootStep         => _seFootStep,
        SeType.StartRoomExit    => _seStartRoomExit,
        SeType.GameOver         => _seGameOver,
        _                       => null,
    };
}
