using UnityEngine;

/// <summary>
/// ゲーム中つねに表示される、ビネットのベースライン強度。
/// 状況によらず一定値を返す、最も基本的な IVignetteSource。
/// （通常プレイ突入後に加算される演出は UsuallyVignette が担当し、本クラスとは責務が別。）
/// </summary>
public class PermanentVignette : MonoBehaviour, IVignetteSource
{
    [SerializeField, Range(0f, 1f)] private float _intensity = 0.25f; // 常時表示するビネット強度

    public float Intensity => _intensity;
}
