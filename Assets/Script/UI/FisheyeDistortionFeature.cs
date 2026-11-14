using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class FisheyeDistortionFeature : ScriptableRendererFeature
{
    [SerializeField] private Material fisheyeMaterial;
    [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

    private static readonly int kBarrelPowerId = Shader.PropertyToID("_BarrelPower");

    private static FisheyeDistortionFeature _current;	// 全画面に 1 つだけの効果なので、値を渡す入口として自身を持つ
    private Material _runtimeMaterial;					// 共有のアセットを書き換えないための実行時の複製
    private FisheyePass _pass;

    public override void Create()
    {
        _runtimeMaterial = fisheyeMaterial != null ? new Material(fisheyeMaterial) : null;
        _pass = new FisheyePass(_runtimeMaterial) { renderPassEvent = renderPassEvent };
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

    private class FisheyePass : ScriptableRenderPass
    {
        private readonly Material _material;

        public FisheyePass(Material material)
        {
            _material = material;
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var source = resourceData.cameraColor;

            var desc = renderGraph.GetTextureDesc(source);
            desc.name = "_FisheyeTarget";
            desc.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(desc);

            RenderGraphUtils.BlitMaterialParameters blitParams = new(source, destination, _material, 0);
            renderGraph.AddBlitPass(blitParams, "Fisheye Distortion");

            resourceData.cameraColor = destination;
        }
    }
}