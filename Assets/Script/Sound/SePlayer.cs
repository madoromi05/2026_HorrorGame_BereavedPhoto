using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// SE 専用プレイヤー。
/// 子 GameObject にプールした AudioSource で複数チャンネルを同時再生する。
/// 空きチャンネルがない場合はラウンドロビンで上書き再生する。
/// </summary>
public class SePlayer : MonoBehaviour
{
    [SerializeField] private AudioMixerGroup _outputGroup;
    [SerializeField] private int _channelCount = 8;

    [Header("3D 音源")]
    [SerializeField] private float _minDistance = 1f;
    [SerializeField] private float _maxDistance = 35f;

    private const float k2DBlend = 0f;
    private const float k3DBlend = 1f;

    private AudioSource[] _sources;
    private int           _robin;

    // ループSE専用の予約チャンネル。プールとは別に1本だけ持つ（砂嵐ノイズなど停止できる持続音用）。
    private AudioSource _loopSource;

    private void Awake()
    {
        _sources = new AudioSource[_channelCount];
        for (int i = 0; i < _channelCount; i++)
            _sources[i] = CreateSource($"SeChannel_{i:00}");

        _loopSource = CreateSource("SeLoopChannel");
        _loopSource.loop = true;
    }

    // 出力先ミキサーグループを揃えた AudioSource を子オブジェクトとして生成する。
    private AudioSource CreateSource(string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(transform, false);
        var src = child.AddComponent<AudioSource>();
        src.playOnAwake = false;
        if (_outputGroup != null)
            src.outputAudioMixerGroup = _outputGroup;
        return src;
    }

    // SE を 2D で再生する。AudioClip / AudioRandomContainer のどちらも再生可能。
    public void Play(AudioResource resource, float volume = 1f, float pitch = 1f)
    {
        var src = PrepareSource(resource, volume, pitch);
        if (src == null) return;
        src.spatialBlend = k2DBlend;
        src.Play();
    }

    // 停止できる持続SEを2Dループ再生する（砂嵐ノイズ用）。
    public void PlayLoop(AudioResource resource, float volume = 1f, float pitch = 1f)
    {
        if (resource == null) return;
        _loopSource.resource     = resource;
        _loopSource.volume       = volume;
        _loopSource.pitch        = pitch;
        _loopSource.spatialBlend = k2DBlend;
        _loopSource.Play();
    }

    // ループSEを即停止する。再生していなければ何もしない。
    public void StopLoop()
    {
        if (_loopSource.isPlaying) _loopSource.Stop();
    }

    // SE を指定座標の 3D 音源として再生する。距離減衰は Linear。
    public void Play3D(AudioResource resource, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        var src = PrepareSource(resource, volume, pitch);
        if (src == null) return;
        src.transform.position = position;
        src.spatialBlend       = k3DBlend;
        src.rolloffMode        = AudioRolloffMode.Linear;
        src.minDistance        = _minDistance;
        src.maxDistance        = _maxDistance;
        src.Play();
    }

    // チャンネルは 2D/3D で共有するため、spatialBlend は各再生側で毎回明示する。
    private AudioSource PrepareSource(AudioResource resource, float volume, float pitch)
    {
        if (resource == null) return null;
        var src = GetFreeSource();
        src.resource = resource;
        src.volume   = volume;
        src.pitch    = pitch;
        return src;
    }

    private AudioSource GetFreeSource()
    {
        foreach (var s in _sources)
            if (!s.isPlaying) return s;

        var fallback = _sources[_robin];
        _robin = (_robin + 1) % _sources.Length;
        return fallback;
    }
}
