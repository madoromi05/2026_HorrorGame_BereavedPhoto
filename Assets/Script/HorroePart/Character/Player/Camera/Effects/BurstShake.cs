using UnityEngine;

/// <summary>
/// 発見された瞬間の強い一回揺れ。始めが最も強く、時間とともに弱まる
/// DetectionCameraEffects が持つ。残り時間とノイズの開始位置だけを持ち、長さ・強さ・速さは呼ぶたびに受け取る
/// </summary>
public class BurstShake
{
    private const float kPerlinSeedRange = 100f;	// ノイズの開始位置をばらす幅
    private const float kVerticalSpeed   = 0.9f;	// 縦のノイズを少し遅らせ、横と同じ揺れ方にしない

    private readonly float _seedX;
    private readonly float _seedY;
    private float _timer;

    public BurstShake()
    {
        _seedX = Random.value * kPerlinSeedRange;
        _seedY = Random.value * kPerlinSeedRange;
    }

    public bool IsPlaying => _timer > 0f;

    public void Play(float duration) => _timer = duration;

    public Vector3 Tick(float duration, float magnitude, float frequency)	// 時間を進めて、今の揺れの角度（度）を返す
    {
        _timer -= Time.deltaTime;
        float decay = Mathf.Max(0f, _timer) / duration;
        float t = Time.time * frequency;
        float x = SignedPerlin(_seedX + t, 0f);
        float y = SignedPerlin(0f, _seedY + t * kVerticalSpeed);
        return new Vector3(x, y, 0f) * (magnitude * decay);
    }

    private static float SignedPerlin(float x, float y) => (Mathf.PerlinNoise(x, y) - 0.5f) * 2f;	// 0〜1 のノイズを -1〜1 に直す
}
