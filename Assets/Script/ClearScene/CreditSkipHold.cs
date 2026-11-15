using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// クレジットのスキップを長押しで受け付ける係
/// GameClearDirector が持ち、入力の押す・離すを Begin・End へつなぎ、毎フレーム調整値を添えて Tick を呼ぶ
/// 押し続けた秒数とプログレスバーの見た目だけを持つ。規定秒数に届いたことを返すだけで、タイトルへ移るのは持ち主
/// </summary>
public class CreditSkipHold
{
    private readonly Image       _progressImage;
    private readonly CanvasGroup _canvasGroup;

    private bool  _isHolding;	// スキップボタンを押している間 true
    private float _holdElapsed;	// 長押し経過秒数

    public CreditSkipHold(Image progressImage, CanvasGroup canvasGroup)
    {
        _progressImage = progressImage;
        _canvasGroup   = canvasGroup;
    }

    // スキップ用プログレスバーは初期状態で非表示（押している間だけフェードインさせる）
    public void HideAtStart()
    {
        _canvasGroup.alpha = 0f;
        _progressImage.fillAmount = 0f;
    }

    // スキップ長押しの開始／終了。押している間だけ Tick() でプログレスを進める
    public void Begin() => _isHolding = true;
    public void End()   => _isHolding = false;

    // 押している間だけプログレスバーをフェードインさせながら進め、
    // 規定秒数まで押し続けたら true を返す（以後も押し続ける間は毎フレーム true）
    // 途中で離したら経過とバーをリセットし、フェードアウトさせる
    // 調整値は持ち主の Inspector から毎回受け取り、ここへは写さない
    public bool Tick(float holdDuration, float fadeDuration, float visibleAlpha)
    {
        float fadeTarget = _isHolding ? visibleAlpha : 0f;
        float fadeStep   = fadeDuration > 0f ? Time.deltaTime / fadeDuration : 1f;
        _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, fadeTarget, fadeStep);

        if (!_isHolding)
        {
            _holdElapsed = 0f;
            _progressImage.fillAmount = 0f;
            return false;
        }

        _holdElapsed += Time.deltaTime;
        _progressImage.fillAmount = _holdElapsed / holdDuration;

        return _holdElapsed >= holdDuration;
    }
}
