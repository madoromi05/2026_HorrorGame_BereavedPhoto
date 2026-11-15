using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FisheyeDistortionFeature : ScriptableRendererFeature
{
    [SerializeField] private Material fisheyeMaterial;
    [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

    private static readonly int kBarrelPowerId = Shader.PropertyToID("_BarrelPower");

    private static FisheyeDistortionFeature _current;	// 全画面に 1 つだけの効果なので、値を渡す入口として自身を持つ
    private Material _runtimeMaterial;					// 共有のアセットを書き換えないための実行時の複製
    private FullscreenBlitPass _pass;

    public override void Create()
    {
        _runtimeMaterial = fisheyeMaterial != null ? new Material(fisheyeMaterial) : null;
        _pass = new FullscreenBlitPass(_runtimeMaterial, targetName: "_FisheyeTarget", passName: "Fisheye Distortion")
        {
            renderPassEvent = renderPassEvent
        };
        _current = this;
    }

    protected override void Dispose(bool disposing)
    {
        if (_runtimeMaterial != null) CoreUtils.Destroy(_runtimeMaterial);
        if (_current == this) _current = null;
    }

    public static void SetBarrelPower(float power)	// 魚眼の強さを設定する。0 なら歪まない
    {
        if (_current == null || _current._runtimeMaterial == null) return;
        _current._runtimeMaterial.SetFloat(kBarrelPowerId, power);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_runtimeMaterial == null) return;
        renderer.EnqueuePass(_pass);
    }
}