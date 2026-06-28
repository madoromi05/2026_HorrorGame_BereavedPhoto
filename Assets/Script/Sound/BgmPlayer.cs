using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// BGM 専用プレイヤー（1チャンネル）。
/// Play / Stop / CrossFade がコルーチンでフェードを管理する。
/// AudioSource.volume でフェードするため、Mixer の BGM グループ音量と独立して動作する。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BgmPlayer : MonoBehaviour
{
    [SerializeField] private AudioMixerGroup _outputGroup;
    [SerializeField] private float _defaultFadeDuration = 0.8f;

    private AudioSource _source;
    private Coroutine   _fadeRoutine;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.loop        = true;
        _source.playOnAwake = false;
        if (_outputGroup != null)
            _source.outputAudioMixerGroup = _outputGroup;
    }

    /// フェードインして再生。
    public void Play(AudioClip clip, float fadeIn = -1f)
    {
        if (clip == null) return;
        StopFade();
        _fadeRoutine = StartCoroutine(
            FadeInRoutine(clip, fadeIn >= 0f ? fadeIn : _defaultFadeDuration));
    }

    /// フェードアウトして停止。
    public void Stop(float fadeOut = -1f)
    {
        if (!_source.isPlaying) return;
        StopFade();
        _fadeRoutine = StartCoroutine(
            FadeOutRoutine(fadeOut >= 0f ? fadeOut : _defaultFadeDuration));
    }

    /// 現在の曲をフェードアウトし、新しい曲をフェードインする。
    public void CrossFade(AudioClip newClip, float crossDuration = -1f)
    {
        if (newClip == null) return;
        if (_source.clip == newClip && _source.isPlaying) return;
        StopFade();
        float dur = crossDuration >= 0f ? crossDuration : _defaultFadeDuration;
        _fadeRoutine = StartCoroutine(CrossFadeRoutine(newClip, dur));
    }

    // ---- コルーチン ----

    private IEnumerator FadeInRoutine(AudioClip clip, float duration)
    {
        _source.clip   = clip;
        _source.volume = 0f;
        _source.Play();
        float t = 0f;
        float inv = 1f / Mathf.Max(duration, 0.001f);
        while (t < 1f)
        {
            t = Mathf.MoveTowards(t, 1f, Time.deltaTime * inv);
            _source.volume = t;
            yield return null;
        }
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        float start = _source.volume;
        float t = 0f;
        float inv = 1f / Mathf.Max(duration, 0.001f);
        while (t < 1f)
        {
            t = Mathf.MoveTowards(t, 1f, Time.deltaTime * inv);
            _source.volume = Mathf.Lerp(start, 0f, t);
            yield return null;
        }
        _source.Stop();
    }

    private IEnumerator CrossFadeRoutine(AudioClip newClip, float duration)
    {
        if (_source.isPlaying)
        {
            float half = duration * 0.5f;
            yield return FadeOutRoutine(half);
            yield return FadeInRoutine(newClip, half);
        } else {
            yield return FadeInRoutine(newClip, duration);
        }
    }

    private void StopFade()
    {
        if (_fadeRoutine == null) return;
        StopCoroutine(_fadeRoutine);
        _fadeRoutine = null;
    }
}
