using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 同一 GameObject 上の各 IVignetteSource を合算し、Post Processing の Vignette へ書き込む唯一の入口。
/// Volume/Vignette の所有と書き込みをここへ集約し、演出の中身は各 Source に委譲する。
/// LateUpdate で合算するため、各 Source の Update 更新が済んだ後の値を使う。
/// </summary>
[RequireComponent(typeof(PermanentVignette))]
public class VignetteCompositor : MonoBehaviour
{
    private const float kMaxIntensity = 1f;   // Vignette.intensity の上限

    [SerializeField] private Volume _volume;

    private Vignette _vignette;
    private IVignetteSource[] _sources;

    private void Awake()
    {
        if (_volume == null)
        {
            DebugCustom.LogError("[VignetteCompositor] _volume が未設定です。", this);
            enabled = false;
            return;
        }
        if (_volume.sharedProfile == null)
        {
            DebugCustom.LogError("[VignetteCompositor] sharedProfile が null です。Volume に Profile を設定してください。", this);
            enabled = false;
            return;
        }

        // sharedProfile を直接書き換えないよう複製し、このインスタンス専用の Vignette を得る。
        _volume.profile = Instantiate(_volume.sharedProfile);

        if (!_volume.profile.TryGet(out _vignette))
            _vignette = _volume.profile.Add<Vignette>(overrides: true);

        _vignette.active = true;
        _vignette.intensity.overrideState = true;

        _sources = GetComponents<IVignetteSource>();
    }

    private void LateUpdate()
    {
        if (_vignette == null) return;

        float total = 0f;
        for (int i = 0; i < _sources.Length; i++)
            total += _sources[i].Intensity;

        _vignette.intensity.value = Mathf.Min(total, kMaxIntensity);
    }
}
