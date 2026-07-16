using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

/// <summary>
/// VHS（ビデオテープ風）ポストエフェクトを画面全体に適用する RendererFeature。
/// 魚眼レンズ（FisheyeDistortionFeature）と同じく RenderGraph の Blit を 1 パス行う。
/// 効果は全画面に 1 つだけのため、シーン別設定の入口として static Apply を公開する。
/// 各シーンの VhsController が自分の VhsProfile をここへ渡す。
/// </summary>
public class VhsFeature : ScriptableRendererFeature
{
    // シェーダー Custom/Vhs のプロパティ ID（文字列直書きを避けるためまとめて保持）。
    private static class PropertyId
    {
        public static readonly int Intensity            = Shader.PropertyToID("_Intensity");
        public static readonly int ChromaticOffset       = Shader.PropertyToID("_ChromaticOffset");
        public static readonly int ScanlineCount         = Shader.PropertyToID("_ScanlineCount");
        public static readonly int ScanlineIntensity     = Shader.PropertyToID("_ScanlineIntensity");
        public static readonly int WaveAmplitude         = Shader.PropertyToID("_WaveAmplitude");
        public static readonly int WaveSpeed             = Shader.PropertyToID("_WaveSpeed");
        public static readonly int NoiseIntensity        = Shader.PropertyToID("_NoiseIntensity");
        public static readonly int TrackingBandHeight    = Shader.PropertyToID("_TrackingBandHeight");
        public static readonly int TrackingBandSpeed     = Shader.PropertyToID("_TrackingBandSpeed");
        public static readonly int TrackingBandIntensity = Shader.PropertyToID("_TrackingBandIntensity");
        public static readonly int Saturation            = Shader.PropertyToID("_Saturation");
        public static readonly int VignettePower         = Shader.PropertyToID("_VignettePower");
        public static readonly int SignalLoss            = Shader.PropertyToID("_SignalLoss");
    }

    [SerializeField] private Material vhsMaterial;
    [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

    // 全画面に 1 つだけの効果なので、シーンから設定を渡す唯一の入口として自身を公開する。
    private static VhsFeature _current;

    // 共有マテリアルアセットを汚さないための実行時インスタンス。
    private Material _runtimeMaterial;
    private VhsPass _pass;

    // 最後にシーンから指定されたプロファイル。Create と Apply の順序に依存しないよう static で保持する。
    private static VhsProfile _activeProfile;

    // 現在のシーンで VHS を描画するか。_activeProfile の内容から決まる。
    private bool _isEnabled;

    public override void Create()
    {
        _runtimeMaterial = vhsMaterial != null ? new Material(vhsMaterial) : null;
        _pass = new VhsPass(_runtimeMaterial) { renderPassEvent = renderPassEvent };
        _current = this;
        ApplyActiveProfile();   // Create が後から走っても、保存済みプロファイルを反映する
    }

    protected override void Dispose(bool disposing)
    {
        if (_runtimeMaterial != null) CoreUtils.Destroy(_runtimeMaterial);
        if (_current == this) _current = null;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // マテリアル未設定、または現在のシーンで無効ならパスごと停止する。
        if (_runtimeMaterial == null || !_isEnabled) return;
        renderer.EnqueuePass(_pass);
    }

    /// <summary>
    /// シーンの VhsController から呼ぶ、設定適用の入口。
    /// 指定プロファイルを保存し、フィーチャ生成済みなら即マテリアルへ反映する。
    /// Create より先に呼ばれても保存分は Create 側で反映される（順序非依存）。
    /// </summary>
    public static void Apply(VhsProfile profile)
    {
        _activeProfile = profile;
        _current?.ApplyActiveProfile();
    }

    /// <summary>
    /// 信号消失（ホワイトノイズ）の進行度を設定する実行時の入口。0 で通常の VHS、1 で完全な砂嵐。
    /// ゲームオーバー演出から毎フレーム呼んで映像を殺す用途を想定している。
    /// プロファイルで VHS を無効にしたシーンでは VHS パス自体が走らないため何も映らない。
    /// </summary>
    public static void SetSignalLoss(float progress)
    {
        _current?.ApplySignalLoss(progress);
    }

    private void ApplySignalLoss(float progress)
    {
        if (_runtimeMaterial == null) return;
        _runtimeMaterial.SetFloat(PropertyId.SignalLoss, Mathf.Clamp01(progress));
    }

    // 保存済みプロファイルを実行時マテリアルへ反映する。
    // profile.enabled=false または未割り当てなら、そのシーンでは VHS を無効化する。
    private void ApplyActiveProfile()
    {
        var p = _activeProfile;
        _isEnabled = p != null && p.enabled;
        if (!_isEnabled || _runtimeMaterial == null) return;

        _runtimeMaterial.SetFloat(PropertyId.Intensity, p.intensity);
        _runtimeMaterial.SetFloat(PropertyId.ChromaticOffset, p.chromaticOffset);
        _runtimeMaterial.SetFloat(PropertyId.ScanlineCount, p.scanlineCount);
        _runtimeMaterial.SetFloat(PropertyId.ScanlineIntensity, p.scanlineIntensity);
        _runtimeMaterial.SetFloat(PropertyId.WaveAmplitude, p.waveAmplitude);
        _runtimeMaterial.SetFloat(PropertyId.WaveSpeed, p.waveSpeed);
        _runtimeMaterial.SetFloat(PropertyId.NoiseIntensity, p.noiseIntensity);
        _runtimeMaterial.SetFloat(PropertyId.TrackingBandHeight, p.trackingBandHeight);
        _runtimeMaterial.SetFloat(PropertyId.TrackingBandSpeed, p.trackingBandSpeed);
        _runtimeMaterial.SetFloat(PropertyId.TrackingBandIntensity, p.trackingBandIntensity);
        _runtimeMaterial.SetFloat(PropertyId.Saturation, p.saturation);
        _runtimeMaterial.SetFloat(PropertyId.VignettePower, p.vignettePower);

        // 実行時マテリアルは RendererFeature 側に属しシーンを跨いで生き残るため、
        // ゲームオーバーで 1 にした信号消失をここで戻さないと次のシーンへ砂嵐が残る。
        // プロファイル適用＝シーン切り替えの契機なので、この位置でリセットする。
        _runtimeMaterial.SetFloat(PropertyId.SignalLoss, 0f);
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
