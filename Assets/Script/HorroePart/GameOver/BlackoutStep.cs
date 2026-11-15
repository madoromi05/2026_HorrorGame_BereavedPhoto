using System.Collections;
using UnityEngine;

/// <summary>
/// ゲームオーバーの段：画面を黒く覆いきり、黒画面を保持する
/// 覆いを作るのは GameOverScreen。この段はフェードと保持の時間だけを持つ
/// </summary>
public class BlackoutStep : IGameOverStep
{
    private readonly float _fadeDuration;
    private readonly float _holdDuration;

    public BlackoutStep(float fadeDuration, float holdDuration)
    {
        _fadeDuration = fadeDuration;
        _holdDuration = holdDuration;
    }

    public IEnumerator Play(GameOverContext context)
    {
        yield return Tween.FadeAlpha(GameOverScreen.CreateBlackout(), 0f, 1f, _fadeDuration);	// OFF 音の余韻も兼ねる
        yield return new WaitForSeconds(_holdDuration);
    }
}
