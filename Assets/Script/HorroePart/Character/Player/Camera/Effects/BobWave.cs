using UnityEngine;

/// <summary>
/// 歩いているときのボブの波を求める。ヘッドボブとライトボブが同じ波を使う
/// DetectionCameraEffects が持つ。ダッシュ中かどうかは PlayerMover から読み、速く大きくする
/// どの軸に当てるかは使う側が決める
/// </summary>
public class BobWave
{
    private const float kMajorSpeed = 2f;		// 大きい側の軸は 2 倍の速さで揺らす
    private const float kMinorRatio = 0.35f;	// 小さい側の軸の大きさ（大きい側に対する比）
    private const float kDashScale  = 1.4f;	// ダッシュ中はボブを速く大きくする

    private readonly PlayerMover _mover;

    public BobWave(PlayerMover mover) => _mover = mover;

    /// <summary>
    /// 今の波を返す。x が大きい側の軸（2 倍の速さ）、y が小さい側の軸
    /// </summary>
    public Vector2 Sample(float magnitude, float frequency)
    {
        float mag = ScaleForDash(magnitude);
        float t   = Time.time * ScaleForDash(frequency);
        return new Vector2(Mathf.Sin(t * kMajorSpeed) * mag, Mathf.Sin(t) * (mag * kMinorRatio));
    }

    private float ScaleForDash(float value)	// ダッシュ中だけ kDashScale 倍にする
        => _mover.CurrentMoveState == PlayerMover.MoveState.Dash ? value * kDashScale : value;
}
