using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class FisheyeDistortionFeature : ScriptableRendererFeature
{
    [SerializeField] private Material fisheyeMaterial;
    [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

    private FisheyePass _pass;

    public override void Create()
    {
        _pass = new FisheyePass(fisheyeMaterial) { renderPassEvent = renderPassEvent };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (fisheyeMaterial == null) return;
        renderer.EnqueuePass(_pass);
    }

    private class FisheyePass : ScriptableRenderPass
    {
        private readonly Material _material;

        public FisheyePass(Material material) => _material = material;

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // テクスチャ取得
            var resourceData = frameData.Get<UniversalResourceData>();
            var source = resourceData.cameraColor;

            // 新しいテクスチャ作成
            var desc = renderGraph.GetTextureDesc(source);
            desc.name = "_FisheyeTarget";
            desc.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(desc);

            // テクスチャのbitの番号を渡す
            RenderGraphUtils.BlitMaterialParameters blitParams = new(source, destination, _material, 0);
            renderGraph.AddBlitPass(blitParams, "Fisheye Distortion");

            // カラーバッファの参照を自動化
            resourceData.cameraColor = destination;
        }
    }
}