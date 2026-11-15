using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 時間をかけて進み具合を 0 から 1 へ進める共通のコルーチン。
/// フェードや演出の補間を、クラスごとに書き直さないために置く。
/// 進み具合は Time.deltaTime で数えるので、timeScale の影響を受ける。
/// </summary>
public static class Tween
{
    /// <summary>
    /// duration 秒かけて、進み具合 t（0〜1）を毎フレーム apply へ渡す。最後に必ず 1 を渡して終わる。
    /// duration が 0 以下なら、待たずに 1 を渡す。
    /// </summary>
    public static IEnumerator Run(float duration, Action<float> apply)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            apply(Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        apply(1f);
    }

    public static IEnumerator FadeAlpha(CanvasGroup group, float from, float to, float duration)	// CanvasGroup の alpha を from から to へ補間する
        => Run(duration, t => group.alpha = Mathf.Lerp(from, to, t));

    public static float EaseInQuad(float t) => t * t;	// 始めはゆっくり、後半で加速する

    public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);	// 始めは速く、最後にゆっくり止まる
}
