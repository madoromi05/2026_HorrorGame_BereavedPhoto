using UnityEngine;

/// <summary>
/// 隠れ状態のときに加算される、脈打つ（揺らぐ）ビネット。
/// フェードイン/アウトと正弦波パルスで「息をひそめる」緊張感を演出する。
/// フェード中はパルスを抑え、フェード完了割合に応じて揺れを大きくする。
/// </summary>
public class HidingPulseVignette : MonoBehaviour, IVignetteSource
{
    [SerializeField] private float _pulseFrequency = 0.8f;                    // パルス周波数（Hz）
    [SerializeField, Range(0f, 0.2f)] private float _pulseAmplitude = 0.06f;  // パルス振幅

    private float _current;
    private float _target;
    private float _fadeSpeed;
    private bool  _isHiding;

    public float Intensity
    {
        get
        {
            // フェードが完了している割合に応じてパルス振幅をスケール（フェード中は揺れない）。
            float fadeRatio = _target > 0f ? _current / _target : 0f;
            float pulse = _isHiding
                ? Mathf.Sin(Time.time * _pulseFrequency * Mathf.PI * 2f) * _pulseAmplitude * fadeRatio
                : 0f;
            return _current + pulse;
        }
    }

    /// <summary>隠れ状態を設定する。true でフェードイン、false でフェードアウト。</summary>
    public void SetHiding(bool isHiding, float intensity, float fadeSpeed)
    {
        _isHiding  = isHiding;
        _target    = isHiding ? intensity : 0f;
        _fadeSpeed = fadeSpeed;
    }

    private void Update()
    {
        _current = Mathf.MoveTowards(_current, _target, _fadeSpeed * Time.deltaTime);
    }
}
