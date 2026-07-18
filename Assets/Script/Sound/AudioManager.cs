using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// サウンド管理のエントリポイント（MonoBehaviour Singleton / DontDestroyOnLoad）。
/// Resources/AudioManager プレハブを RuntimeInitializeOnLoadMethod で自動生成する。
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ゲームオーバー確定後、ゲームオーバー SE 以外の SE 再生を抑止する状態。
    // シーンをまたいで持続しないよう、次シーンのロードで自動解除する。
    private bool _seSuppressed;

    /// SE 抑止中か（EnemySoundPlayer など AudioManager を経由しない音源が参照する）。
    public bool IsSeSuppressed => _seSuppressed;

    [Header("サブシステム")]
    [SerializeField] private BgmPlayer       _bgmPlayer;
    [SerializeField] private SePlayer        _sePlayer;
    [SerializeField] private AudioMixerProxy _mixerProxy;

    [Header("BGM クリップ")]
    [SerializeField] private AudioClip _bgmTitle;
    [SerializeField] private AudioClip _bgmGameNormal;
    [SerializeField] private AudioClip _bgmGameChase;
    [SerializeField] private AudioClip _bgmEnding;

    [Header("SE クリップ（AudioClip / AudioRandomContainer どちらも可）")]
    [SerializeField] private AudioResource _seFootStep;
    [SerializeField] private AudioResource _seEnemyMother;
    [SerializeField] private AudioResource _seEnemyFather;
    [SerializeField] private AudioResource _seItemPickup;
    [SerializeField] private AudioResource _seMemoPageTurn;
    [SerializeField] private AudioResource _seAnalysisComplete;
    [SerializeField] private AudioResource _seStartRoomExit;
    [SerializeField] private AudioResource _seGameOver;
    [SerializeField] private AudioResource _seHandLightToggle;
    [SerializeField] private AudioResource _seHandLightFlicker;
    [SerializeField] private AudioResource _seDoorOpen;
    [SerializeField] private AudioResource _seDoorClose;
    [SerializeField] private AudioResource _seObstructionItemHit;
    [SerializeField] private AudioResource _seSignalNoise;   // ゲームオーバー時のホワイトノイズ
    [SerializeField] private AudioResource _seTvPowerOff;     // テレビを消す音

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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 次シーンへ移った時点で SE 抑止を解除する（遷移先の SE を巻き込まないため）。
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => _seSuppressed = false;

    // ---- ゲームオーバー ----
    /// ゲームオーバー確定時の音声処理。BGM を即停止し、ゲームオーバー SE を鳴らしてから
    /// 以降の SE 再生を抑止する（抑止は次シーンのロードで自動解除）。
    public void EnterGameOver(SeType gameOverSe)
    {
        StopBgm(0f);
        PlaySe(gameOverSe);   // 抑止フラグを立てる前に鳴らすことで、この SE だけは再生する
        _seSuppressed = true;
    }

    // ---- ゲームオーバー演出音（SE 抑止をバイパスして鳴らす）----
    /// ホワイトノイズ（砂嵐）のループ再生を開始する。
    public void StartGameOverNoise(float pitch = 1f)
    {
        var resource = TryGetSeResource(SeType.SignalNoise);
        if (resource != null) _sePlayer.PlayLoop(resource, 1f, pitch);
    }

    /// ホワイトノイズを即停止する（テレビを消す瞬間に呼ぶ）。
    public void StopGameOverNoise() => _sePlayer.StopLoop();

    /// テレビ OFF 音など、演出音を抑止を無視して 1 回鳴らす。
    public void PlayGameOverSe(SeType type)
    {
        var resource = TryGetSeResource(type);
        if (resource != null) _sePlayer.Play(resource);
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
    /// SE を 2D で再生する（複数同時可）。
    public void PlaySe(SeType type, float volume = 1f, float pitch = 1f)
    {
        if (_seSuppressed) return;
        var resource = TryGetSeResource(type);
        if (resource != null) _sePlayer.Play(resource, volume, pitch);
    }

    /// SE を指定座標の 3D 音源として再生する（複数同時可）。
    public void PlaySe3D(SeType type, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        if (_seSuppressed) return;
        var resource = TryGetSeResource(type);
        if (resource != null) _sePlayer.Play3D(resource, position, volume, pitch);
    }

    private AudioResource TryGetSeResource(SeType type)
    {
        if (type == SeType.None) return null;
        var resource = GetSeResource(type);
        if (resource == null)
            DebugCustom.LogWarning($"[AudioManager] SE クリップ未設定: {type}");
        return resource;
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
        _                  => null,
    };

    public AudioResource GetSeResource(SeType type) => type switch
    {
        SeType.EnemyMother      => _seEnemyMother,
        SeType.EnemyFather      => _seEnemyFather,
        SeType.ItemPickup       => _seItemPickup,
        SeType.MemoPageTurn     => _seMemoPageTurn,
        SeType.AnalysisComplete => _seAnalysisComplete,
        SeType.FootStep         => _seFootStep,
        SeType.StartRoomExit    => _seStartRoomExit,
        SeType.GameOver         => _seGameOver,
        SeType.HandLightToggle  => _seHandLightToggle,
        SeType.HandLightFlicker => _seHandLightFlicker,
        SeType.DoorOpen         => _seDoorOpen,
        SeType.DoorClose        => _seDoorClose,
        SeType.ObstructionItemHit => _seObstructionItemHit,
        SeType.SignalNoise        => _seSignalNoise,
        SeType.TvPowerOff         => _seTvPowerOff,
        _                       => null,
    };
}
