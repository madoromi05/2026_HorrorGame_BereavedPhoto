using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// エンディングの段：閉じたドアを見せてから開いたドアへ差し替え、開く音を聞かせきる
/// GameClearDirector が持ち、段の鎖の先頭として流す。ドアの絵を舞台から下ろすのも持ち主から呼ばれてここが行う
/// 白転・BGM・クレジットは扱わない
/// </summary>
public class DoorOpenStep : IEndingStep
{
    private readonly Image  _doorImage;
    private readonly Sprite _doorClosed;
    private readonly Sprite _doorOpened;
    private readonly float  _initialShowDuration;
    private readonly float  _afterDoorOpenWait;

    public DoorOpenStep(Image doorImage, Sprite doorClosed, Sprite doorOpened, float initialShowDuration, float afterDoorOpenWait)
    {
        _doorImage           = doorImage;
        _doorClosed          = doorClosed;
        _doorOpened          = doorOpened;
        _initialShowDuration = initialShowDuration;
        _afterDoorOpenWait   = afterDoorOpenWait;
    }

    public void ShowClosed() => _doorImage.sprite = _doorClosed;	// 演出の始まりに見せる 1 枚目

    public void Hide() => _doorImage.gameObject.SetActive(false);	// 白で覆われている間に呼ぶ

    public IEnumerator Play()
    {
        yield return new WaitForSeconds(_initialShowDuration);	// 1枚目

        _doorImage.sprite = _doorOpened;	// 2枚目
        yield return PlaySeAndWait(SeType.DoorOpen, _afterDoorOpenWait);
    }

    // SE を鳴らし、そのクリップ長＋余韻ぶん待機する。クリップ未設定ならクリップ長は 0 扱い
    private static IEnumerator PlaySeAndWait(SeType type, float extraWait)
    {
        AudioManager.Instance.PlaySe(type);
        var clip = AudioManager.Instance.GetSeResource(type) as AudioClip;
        float clipLength = clip != null ? clip.length : 0f;
        yield return new WaitForSeconds(clipLength + extraWait);
    }
}
