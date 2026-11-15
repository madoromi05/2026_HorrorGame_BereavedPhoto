using UnityEngine;

/// <summary>
/// 音量スライダーの値（0〜1）を、実際の音（AudioListener と AudioMixer）へ映す係
/// OptionMenuController と VolumeSliders が呼ぶ。設定の保存とスライダーの操作はしない
/// </summary>
public static class VolumeApplier
{
    private const float kVolumeBoost = 2f;	// スライダー 0.5 でフル音量にする倍率

    public static void ApplyMaster(float sliderValue) => AudioListener.volume = ToAppliedVolume(sliderValue);

    public static void ApplyBgm(float sliderValue) => AudioManager.Instance?.SetBgmVolume(ToAppliedVolume(sliderValue));

    public static void ApplySe(float sliderValue) => AudioManager.Instance?.SetSeVolume(sliderValue);

    public static void ApplySaved()	// 保存されている音量を、そのまま音へ映す
    {
        ApplyMaster(GameSettings.MasterVolume);
        ApplyBgm(GameSettings.BgmVolume);
        ApplySe(GameSettings.SeVolume);
    }

    /// <summary>
    /// スライダー値（0〜1、既定 0.5）を再生音量に変換する
    /// 既定の 0.5 でフル音量になるよう 2 倍する。0.5〜1.0 はフル音量のまま
    /// </summary>
    private static float ToAppliedVolume(float sliderValue) => Mathf.Min(sliderValue * kVolumeBoost, 1f);
}
