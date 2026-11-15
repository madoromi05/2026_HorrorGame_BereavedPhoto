using System;
using UnityEngine;

/// <summary>
/// プレイヤーが選んだ設定（音量・マウス感度）を PlayerPrefs に読み書きする係
/// 保存のキーと既定値をここだけに置き、オプション画面とゲームプレイの両方がここを読む
/// </summary>
public static class GameSettings
{
    private const string kKeyMaster          = "MasterVolume";
    private const string kKeyBgm             = "BgmVolume";
    private const string kKeySe              = "SeVolume";
    private const string kKeySens            = "MouseSensitivity";
    private const float  kDefaultVolume      = 0.5f;	// 未保存のときの音量。スライダー 0〜1 の真ん中
    private const float  kDefaultSensitivity = 0.1f;	// 未保存のときの感度

    public const float kMaxSensitivity = 0.5f;	// 感度の上限。両方のシーンの感度スライダーがこの値に従う

    public static event Action<float> OnSensitivityChanged;	// ゲーム中のカメラと移動へすぐ伝える

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(kKeyMaster, kDefaultVolume);
        set => PlayerPrefs.SetFloat(kKeyMaster, value);
    }

    public static float BgmVolume
    {
        get => PlayerPrefs.GetFloat(kKeyBgm, kDefaultVolume);
        set => PlayerPrefs.SetFloat(kKeyBgm, value);
    }

    public static float SeVolume
    {
        get => PlayerPrefs.GetFloat(kKeySe, kDefaultVolume);
        set => PlayerPrefs.SetFloat(kKeySe, value);
    }

    public static float Sensitivity
    {
        get => ClampSensitivity(PlayerPrefs.GetFloat(kKeySens, kDefaultSensitivity));
        set
        {
            float clamped = ClampSensitivity(value);
            PlayerPrefs.SetFloat(kKeySens, clamped);
            OnSensitivityChanged?.Invoke(clamped);
        }
    }

    private static float ClampSensitivity(float value) => Mathf.Clamp(value, 0f, kMaxSensitivity);	// 上限を下げる前に保存した値も範囲に収める
}
