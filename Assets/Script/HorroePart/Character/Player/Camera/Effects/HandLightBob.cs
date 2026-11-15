using UnityEngine;

/// <summary>
/// 手のライトへボブのずれを書き込む
/// HandLightController があればそこへ渡し、ライトの GameObject があればその位置もずらす（両方あれば両方へ書く）
/// DetectionCameraEffects が持つ。ずれの大きさや波は持たない
/// </summary>
public class HandLightBob
{
    private readonly HandLightController _controller;
    private readonly Transform _light;
    private readonly Vector3 _initialLocalPos;

    public HandLightBob(HandLightController controller, GameObject light)
    {
        _controller = controller;
        _light      = light != null ? light.transform : null;
        if (_light != null)
            _initialLocalPos = _light.localPosition;
    }

    public bool HasTarget => _controller != null || _light != null;

    public void Set(Vector3 offset)
    {
        if (_controller != null) _controller.SetBobOffset(offset);
        if (_light != null) _light.localPosition = _initialLocalPos + offset;
    }

    public void Reset()	// ずれを無くし、ライトを元の位置へ戻す
    {
        if (_controller != null) _controller.SetBobOffset(Vector3.zero);
        if (_light != null) _light.localPosition = _initialLocalPos;
    }
}
