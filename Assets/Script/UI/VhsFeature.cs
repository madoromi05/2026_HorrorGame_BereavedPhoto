using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

/// <summary>
/// VHS（ビデオテープ風）ポストエフェクトを画面全体に適用する RendererFeature。
/// 魚眼レンズ（FisheyeDistortionFeature）と同じく RenderGraph の Blit を 1 パス行う。
/// 見た目の調整は割り当てたマテリアル（Custom/Vhs）側で行う。
/// </summary>
public class VhsFeature : ScriptableRendererFeature
{
    [SerializeField] private Material vhsMaterial;
    [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

    private VhsPass _pass;

    public override void Create()
    {
        _pass = new VhsPass(vhsMaterial) { renderPassEvent = renderPassEvent };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (vhsMaterial == null) return;
        renderer.EnqueuePass(_pass);
    }

    private class VhsPass : ScriptableRenderPass
    {
        private readonly Material _material;

        public VhsPass(Material material) => _material = material;

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // カメラのカラーテクスチャを取得する。
            var resourceData = frameData.Get<UniversalResourceData>();
            var source = resourceData.cameraColor;

            // 書き込み先テクスチャを同じ設定で作成する（Blit は同一テクスチャの読み書き不可のため）。
            var desc = renderGraph.GetTextureDesc(source);
            desc.name = "_VhsTarget";
            desc.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(desc);

            // マテリアルの Pass 0 で source → destination にフルスクリーン Blit する。
            RenderGraphUtils.BlitMaterialParameters blitParams = new(source, destination, _material, 0);
            renderGraph.AddBlitPass(blitParams, "VHS Effect");

            // 以降のパスがこの結果を参照するよう、カメラカラーの参照を差し替える。
            resourceData.cameraColor = destination;
        }
    }
}
