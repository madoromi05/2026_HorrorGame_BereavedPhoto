using System.Collections;
using UnityEngine;

/// <summary>
/// ゲームオーバーの段：ホワイトノイズを鳴らし、VHS の信号消失を 0→1 にして砂嵐にする
/// 保持してからノイズを止め、テレビを消す音を鳴らす
/// </summary>
public class SignalLossStep : IGameOverStep
{
    private readonly float _lossDuration;
    private readonly float _holdDuration;
    private readonly float _noisePitch;

    public SignalLossStep(float lossDuration, float holdDuration, float noisePitch)
    {
        _lossDuration = lossDuration;
        _holdDuration = holdDuration;
        _noisePitch   = noisePitch;
    }

    public IEnumerator Play(GameOverContext context)
    {
        AudioManager.Instance?.StartGameOverNoise(_noisePitch);
        yield return Tween.Run(_lossDuration, VhsFeature.SetSignalLoss);
        yield return new WaitForSeconds(_holdDuration);				// 砂嵐＋ノイズ保持

        AudioManager.Instance?.StopGameOverNoise();					// ノイズをブツッと停止
        AudioManager.Instance?.PlayGameOverSe(SeType.TvPowerOff);	// テレビOFF音
    }
}
