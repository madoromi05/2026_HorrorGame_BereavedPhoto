using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// AudioMixer のパラメーター操作を一箇所に集約するラッパー。
/// Inspector で AudioMixer を未設定のまま使ってもエラーにならない。
/// </summary>
public class AudioMixerProxy : MonoBehaviour
{
    [SerializeField] private AudioMixer _mixer;

    private const string kBgmParam = "BGM";
    private const string kSeParam  = "SE";

    private float _bgmVolume = 1f;
    private float _seVolume  = 1f;
    private bool _bgmMuted = false;
    private bool _seMuted = false;
    // ---- ボリューム設定（0〜1 正規化値） ----

    public void SetBgmVolume(float normalized)
    {
        _bgmVolume = Mathf.Clamp01(normalized);
        if (!_bgmMuted)
            _mixer?.SetFloat(kBgmParam, ToDb(_bgmVolume));
    }

    public void SetSeVolume(float normalized)
    {
        _seVolume = Mathf.Clamp01(normalized);
        if (!_seMuted)
            _mixer?.SetFloat(kSeParam, ToDb(_seVolume));
    }

    public float GetBgmVolume() => _bgmVolume;
    public float GetSeVolume()  => _seVolume;

    // ---- ミュート ----
    public void MuteBgm(bool mute)
    {
        _bgmMuted = mute;
        _mixer?.SetFloat(kBgmParam, mute ? -80f : ToDb(_bgmVolume));
    }

    public void MuteSe(bool mute)
    {
        _seMuted = mute;
        _mixer?.SetFloat(kSeParam, mute ? -80f : ToDb(_seVolume));
    }
    // ---- ユーティリティ ----

    private static float ToDb(float v) => Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f;
}
