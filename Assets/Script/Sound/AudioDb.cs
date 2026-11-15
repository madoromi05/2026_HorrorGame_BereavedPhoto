using UnityEngine;

/// <summary>
/// デシベルと音量（0〜1）を変換する係
/// AudioMixer は dB、AudioSource.volume は 0〜1 で受け取るので、その間をここで直す
/// </summary>
public static class AudioDb
{
    public const float kSilentDb = -80f;			// AudioMixer の下限。これで無音になる

    private const float kMinLinear   = 0.0001f;	// これより小さい音量は -80dB とみなす。log10 が -∞ にならないように
    private const float kDbPerDecade = 20f;		// 振幅が 10 倍で +20dB
    private const float kDecade      = 10f;

    public static float LinearToDb(float linear) => Mathf.Log10(Mathf.Max(linear, kMinLinear)) * kDbPerDecade;

    public static float DbToLinear(float db) => Mathf.Pow(kDecade, db / kDbPerDecade);
}
