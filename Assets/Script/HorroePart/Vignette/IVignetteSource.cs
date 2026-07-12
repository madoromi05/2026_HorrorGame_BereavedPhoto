/// <summary>
/// ビネット強度に寄与する要素の共通インターフェース。
/// VignetteCompositor が全実装を集約し、Intensity を合算して Volume へ書き込む。
/// 「解析」「隠れ」など演出ごとにクラスを分け、各自の寄与だけを返す責務に限定する。
/// </summary>
public interface IVignetteSource
{
    /// <summary>現在フレームでこの要素が加算するビネット強度（0 以上）。</summary>
    float Intensity { get; }
}
