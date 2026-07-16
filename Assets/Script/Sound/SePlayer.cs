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

    private void Awake()
    {
        _sources = new AudioSource[_channelCount];
        for (int i = 0; i < _channelCount; i++)
        {
            var child = new GameObject($"SeChannel_{i:00}");
            child.transform.SetParent(transform, false);
            var src = child.AddComponent<AudioSource>();
            src.playOnAwake = false;
            if (_outputGroup != null)
                src.outputAudioMixerGroup = _outputGroup;
            _sources[i] = src;
        }
    }

    // SE を 2D で再生する。AudioClip / AudioRandomContainer のどちらも再生可能。
    public void Play(AudioResource resource, float volume = 1f, float pitch = 1f)
    {
        var src = PrepareSource(resource, volume, pitch);
        if (src == null) return;
        src.spatialBlend = k2DBlend;
        src.Play();
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
