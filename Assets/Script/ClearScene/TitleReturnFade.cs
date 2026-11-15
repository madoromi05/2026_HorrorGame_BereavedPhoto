using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// クリア画面からタイトルへ戻る係：全画面を黒く覆いきってからタイトルシーンをロードする
/// GameClearDirector が持ち、いつ戻るか・二重に流さないかは持ち主が決める
/// </summary>
public class TitleReturnFade
{
    private readonly CanvasGroup _darkenOverlay;

    public TitleReturnFade(CanvasGroup darkenOverlay) => _darkenOverlay = darkenOverlay;

    public void HideAtStart() => _darkenOverlay.alpha = 0f;	// タイトル遷移時まで使わないため、初回描画前に透明を確定させる

    public IEnumerator Play(float darkenDuration)	// 暗転の長さは戻り始めるときに持ち主から受け取る
    {
        _darkenOverlay.blocksRaycasts = true; // 暗転中の入力を遮断する
        yield return Tween.FadeAlpha(_darkenOverlay, 0f, 1f, darkenDuration);
        SceneManager.LoadScene(GameProgressManager.SceneTitleName);
    }
}
