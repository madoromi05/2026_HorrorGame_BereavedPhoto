using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

/// <summary>
/// カメラの色をマテリアルで全画面に 1 回 Blit し、結果を後のパスへ渡す RenderPass
/// FisheyeDistortionFeature と VhsFeature が、それぞれ自分のマテリアルで 1 つずつ持つ
/// </summary>
public class FullscreenBlitPass : ScriptableRenderPass
{
    private const int kShaderPass = 0;	// マテリアルの最初の Pass で描く

    private readonly Material _material;
    private readonly string   _targetName;	// 書き込み先テクスチャの名前。Frame Debugger で見分けるため
    private readonly string   _passName;	// Render Graph Viewer に出るパスの名前

    public FullscreenBlitPass(Material material, string targetName, string passName)
    {
        _material   = material;
        _targetName = targetName;
        _passName   = passName;
        requiresIntermediateTexture = true;	// カメラの色を読むので、バックバッファではなく中間テクスチャへ描かせる
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        var source       = resourceData.cameraColor;

        var desc = renderGraph.GetTextureDesc(source);	// 同じテクスチャへは読み書きできないので、同じ設定で書き込み先を作る
        desc.name        = _targetName;
        desc.clearBuffer = false;
        TextureHandle destination = renderGraph.CreateTexture(desc);

        var blitParams = new RenderGraphUtils.BlitMaterialParameters(source, destination, _material, kShaderPass);
        renderGraph.AddBlitPass(blitParams, _passName);

        resourceData.cameraColor = destination;	// 後のパスがこの結果を読むよう差し替える
    }
}
