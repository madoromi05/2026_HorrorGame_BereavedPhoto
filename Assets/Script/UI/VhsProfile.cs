using UnityEngine;

/// <summary>
/// VHS ポストエフェクトのシーン別プリセット。
/// 各値はシェーダー Custom/Vhs のプロパティに対応し、enabled で有効/無効を切り替える。
/// シーンごとにこのアセットを作り、VhsController に割り当てて使う。
/// </summary>
[CreateAssetMenu(fileName = "VhsProfile", menuName = "PostEffect/VHS Profile")]
public class VhsProfile : ScriptableObject
{
    // このプロファイルを適用したシーンで VHS を有効にするか。false なら GPU パスごと停止する。
    public bool enabled = true;

    // 既定値はシェーダー Custom/Vhs の既定値に合わせている。
    [Range(0, 1)]     public float intensity = 1.0f;
    [Range(0, 0.02f)] public float chromaticOffset = 0.004f;
    public float scanlineCount = 480f;
    [Range(0, 1)]     public float scanlineIntensity = 0.25f;
    [Range(0, 0.02f)] public float waveAmplitude = 0.003f;
    public float waveSpeed = 5.0f;
    [Range(0, 1)]     public float noiseIntensity = 0.15f;
    [Range(0, 0.5f)]  public float trackingBandHeight = 0.1f;
    public float trackingBandSpeed = 0.2f;
    [Range(0, 1)]     public float trackingBandIntensity = 0.6f;
    [Range(0, 1)]     public float saturation = 0.75f;
    [Range(0, 2)]     public float vignettePower = 0.4f;
}
