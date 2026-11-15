using System.Collections;
using UnityEngine;

/// <summary>
/// SE 音量スライダーが止まった直後に、確認用の SE を 1 回だけ鳴らす係（デバウンス）
/// OptionMenuController が作り、SE スライダーの変化ごとに Inspector の値と一緒に Request を呼ぶ
/// コルーチンは持ち主の MonoBehaviour で動かす。待っている再生だけを持ち、調整値は持たない。音量の反映と保存はしない
/// </summary>
public class SePreviewDebouncer
{
    private readonly MonoBehaviour _host;

    private Coroutine _pending;

    public SePreviewDebouncer(MonoBehaviour host) => _host = host;

    public void Request(float delay, SeType seType)	// 待っている再生を取り消し、待ち直す
    {
        if (_pending != null) _host.StopCoroutine(_pending);
        _pending = _host.StartCoroutine(PlayAfterDelay(delay, seType));
    }

    private IEnumerator PlayAfterDelay(float delay, SeType seType)
    {
        yield return new WaitForSecondsRealtime(delay);
        AudioManager.Instance?.PlaySe(seType);
        _pending = null;
    }
}
