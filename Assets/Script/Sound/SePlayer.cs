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

    // SE を再生する。volume は AudioSource の音量スケール（0〜1）
    public void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        var src = GetFreeSource();
        src.pitch = pitch;
        src.PlayOneShot(clip, volume);
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
