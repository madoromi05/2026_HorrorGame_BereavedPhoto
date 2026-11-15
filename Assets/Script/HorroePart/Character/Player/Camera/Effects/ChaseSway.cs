using UnityEngine;

/// <summary>
/// 追跡中に移動しているときの軽いカメラの揺れを求める係
/// 状態を持たず、大きさと速さを受け取って今の角度（度）を返すだけ
/// </summary>
public static class ChaseSway
{
    private const float kXSpeed = 1.1f;	// 横揺れの速さ（基準の周波数に対する比）
    private const float kYSpeed = 0.7f;	// 縦揺れの速さ（同上）
    private const float kYRatio = 0.4f;	// 縦揺れの大きさ（横に対する比）

    public static Vector3 Compute(float magnitude, float frequency)
    {
        float t = Time.time * frequency;
        float x = Mathf.Sin(t * kXSpeed) * magnitude;
        float y = Mathf.Abs(Mathf.Sin(t * kYSpeed)) * (magnitude * kYRatio);
        return new Vector3(x, y, 0f);
    }
}
