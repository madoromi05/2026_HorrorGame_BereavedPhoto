using UnityEngine;

/// <summary>
/// スタートルームを出て通常プレイに入った後、常時加算されるビネット。
/// Activate() で一度フェードインし、以降は最大強度を維持する。
/// トリガーは StartRoomBoundary（スタートルーム退出）から呼ばれる。
/// </summary>
public class UsuallyVignette : MonoBehaviour, IVignetteSource
{
    [SerializeField, Range(0f, 0.5f)] private float _intensity = 0.25f; // 通常プレイ時に加算する強度
    [SerializeField] private float _fadeInSpeed = 6f;                   // フェードインの速さ

    private float _current;
    private bool  _isActive;

    public float Intensity => _current;

    /// <summary>スタートルーム退出時に呼ぶ。ビネットをフェードインさせ、以降は維持する。</summary>
    public void Activate()
    {
        _isActive = true;
    }

    private void Update()
    {
        if (!_isActive) return;

        _current = Mathf.MoveTowards(_current, _intensity, _fadeInSpeed * Time.deltaTime);
    }
}
