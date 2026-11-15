using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// BGM・SE の種類から、鳴らす音の素材を引く表
/// AudioManager が Awake で作り、Inspector に置いた素材を種類ごとに登録する
/// 素材の参照は AudioManager のプレハブが持つ。この表は参照を写して引くだけで、鳴らす・抑止するはしない
/// </summary>
public class SoundLibrary
{
    private readonly Dictionary<BgmType, AudioClip>     _bgmClips     = new Dictionary<BgmType, AudioClip>();
    private readonly Dictionary<SeType, AudioResource>  _seResources  = new Dictionary<SeType, AudioResource>();

    public void AddBgm(BgmType type, AudioClip clip) => _bgmClips[type] = clip;

    public void AddSe(SeType type, AudioResource resource) => _seResources[type] = resource;

    public AudioClip GetBgm(BgmType type) => _bgmClips.TryGetValue(type, out var clip) ? clip : null;	// 登録していない種類は null

    public AudioResource GetSe(SeType type) => _seResources.TryGetValue(type, out var resource) ? resource : null;	// 登録していない種類は null
}
