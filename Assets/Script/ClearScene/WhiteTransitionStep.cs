using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// エンディングの段：画面を白で覆い、覆っている間に舞台を差し替えさせてから白を引く
/// 白は「終着点」ではなく「通過点」。何を差し替えるかは知らず、持ち主（GameClearDirector）が渡す処理を呼ぶだけ
/// 覆い始めと重ねて鳴らす SE も持ち主が決める
/// </summary>
public class WhiteTransitionStep : IEndingStep
{
    private readonly CanvasGroup _whiteOverlay;
    private readonly SeType      _coverSe;
    private readonly float       _whitenDuration;
    private readonly float       _holdWhiteDuration;
    private readonly float       _whiteFadeOutDuration;
    private readonly Action      _swapStage;	// 白で覆いきっている間に 1 回だけ呼ぶ

    public WhiteTransitionStep(CanvasGroup whiteOverlay, SeType coverSe,
        float whitenDuration, float holdWhiteDuration, float whiteFadeOutDuration, Action swapStage)
    {
        _whiteOverlay         = whiteOverlay;
        _coverSe              = coverSe;
        _whitenDuration       = whitenDuration;
        _holdWhiteDuration    = holdWhiteDuration;
        _whiteFadeOutDuration = whiteFadeOutDuration;
        _swapStage            = swapStage;
    }

    public void HideAtStart() => _whiteOverlay.alpha = 0f;	// 白転はドアが開いた後に始めるため、初回描画フレームから透明を確定させる

    public IEnumerator Play()
    {
        AudioManager.Instance.PlaySe(_coverSe);
        yield return Tween.FadeAlpha(_whiteOverlay, 0f, 1f, _whitenDuration);
        yield return new WaitForSeconds(_holdWhiteDuration);

        _swapStage();
        yield return Tween.FadeAlpha(_whiteOverlay, 1f, 0f, _whiteFadeOutDuration);
    }
}
