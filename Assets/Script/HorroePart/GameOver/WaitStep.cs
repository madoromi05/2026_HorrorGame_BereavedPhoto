using System.Collections;
using UnityEngine;

/// <summary>
/// ゲームオーバーの段：何もせず決まった秒数だけ待つ
/// </summary>
public class WaitStep : IGameOverStep
{
    private readonly float _seconds;

    public WaitStep(float seconds) => _seconds = seconds;

    public IEnumerator Play(GameOverContext context)
    {
        yield return new WaitForSeconds(_seconds);
    }
}
