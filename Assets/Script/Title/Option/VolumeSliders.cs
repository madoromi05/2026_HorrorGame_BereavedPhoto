using System;
using UnityEngine.UI;

/// <summary>
/// マスター・BGM・SE の音量スライダー 3 本を、設定（GameSettings）と音へつなぐ係
/// OptionMenuController が初期化のときに作り、Bind・Refresh を呼ぶ
/// 音へ映す計算は VolumeApplier、SE を変えた後の試し鳴らしは持ち主が渡す onSeChanged に任せる。パネルの開閉はしない
/// </summary>
public class VolumeSliders
{
    private readonly Slider             _masterSlider;
    private readonly Slider             _bgmSlider;
    private readonly Slider             _seSlider;
    private readonly Action             _onSeChanged;

    public VolumeSliders(Slider masterSlider, Slider bgmSlider, Slider seSlider, Action onSeChanged)
    {
        _masterSlider = masterSlider;
        _bgmSlider    = bgmSlider;
        _seSlider     = seSlider;
        _onSeChanged  = onSeChanged;
    }

    public void Bind()
    {
        _masterSlider?.onValueChanged.AddListener(OnMasterChanged);
        _bgmSlider?.onValueChanged.AddListener(OnBgmChanged);
        _seSlider?.onValueChanged.AddListener(OnSeChanged);
    }

    public void Refresh()	// 保存値をスライダーへ映す。音は鳴らさない
    {
        _masterSlider?.SetValueWithoutNotify(GameSettings.MasterVolume);
        _bgmSlider?.SetValueWithoutNotify(GameSettings.BgmVolume);
        _seSlider?.SetValueWithoutNotify(GameSettings.SeVolume);
    }

    private void OnMasterChanged(float v)
    {
        VolumeApplier.ApplyMaster(v);
        GameSettings.MasterVolume = v;
    }

    private void OnBgmChanged(float v)
    {
        VolumeApplier.ApplyBgm(v);
        GameSettings.BgmVolume = v;
    }

    private void OnSeChanged(float v)
    {
        VolumeApplier.ApplySe(v);
        GameSettings.SeVolume = v;

        _onSeChanged();	// スライダーが止まった直後に1回だけ SE を再生
    }
}
