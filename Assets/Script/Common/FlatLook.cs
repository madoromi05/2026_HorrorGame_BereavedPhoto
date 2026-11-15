using UnityEngine;

/// <summary>
/// 水平面だけで向きを求める係
/// 敵やプレイヤーを、上下に傾けずに相手の方へ向けるときに使う
/// </summary>
public static class FlatLook
{
    private const float kMinSqrLength = 0.001f;	// これより短い向き（長さの 2 乗）は定まらないので回さない

    public static bool TryGetRotation(Vector3 direction, out Quaternion rotation)
    {
        direction.y = 0f;
        rotation = default;
        if (direction.sqrMagnitude <= kMinSqrLength) return false;

        rotation = Quaternion.LookRotation(direction);
        return true;
    }
}
