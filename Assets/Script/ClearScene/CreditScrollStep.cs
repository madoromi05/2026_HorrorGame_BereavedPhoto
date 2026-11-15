using System.Collections;
using UnityEngine;

/// <summary>
/// エンディングの段：クレジットを下から上へスクロールさせる
/// GameClearDirector が持つ。クレジットを隠す・開始位置へ送る・流すことだけをする
/// スキップの長押しは CreditSkipHold が扱い、この段は知らない
/// </summary>
public class CreditScrollStep : IEndingStep
{
    private readonly RectTransform _creditRoot;
    private readonly float         _scrollStartY;
    private readonly float         _scrollEndY;
    private readonly float         _scrollSpeed;

    public CreditScrollStep(RectTransform creditRoot, float scrollStartY, float scrollEndY, float scrollSpeed)
    {
        _creditRoot   = creditRoot;
        _scrollStartY = scrollStartY;
        _scrollEndY   = scrollEndY;
        _scrollSpeed  = scrollSpeed;
    }

    // クレジットは Canvas 上でドア画像より手前に描画されるため、
    // 開始位置へ送るまで無効化しておく（ドア演出中に文字が透けて見えるのを防ぐ）
    public void HideUntilStart() => _creditRoot.gameObject.SetActive(false);

    // クレジットを画面下の開始位置へ送る。白で覆われている間に呼び、瞬間移動を見せない
    public void MoveToStart()
    {
        _creditRoot.gameObject.SetActive(true);
        Vector2 pos = _creditRoot.anchoredPosition;
        pos.y = _scrollStartY;
        _creditRoot.anchoredPosition = pos;
    }

    // クレジットを下から上へスクロールする。終了 Y 到達で終わる（スキップは長押しで別途処理）
    public IEnumerator Play()
    {
        Vector2 pos = _creditRoot.anchoredPosition;

        while (pos.y < _scrollEndY)
        {
            pos.y += _scrollSpeed * Time.deltaTime;
            _creditRoot.anchoredPosition = pos;
            yield return null;
        }
    }
}
