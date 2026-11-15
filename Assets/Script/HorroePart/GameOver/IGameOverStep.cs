using System.Collections;

/// <summary>
/// ゲームオーバー演出の 1 段
/// GameOverHandler が段を順に並べて持ち、前の段が終わってから次の段を流す（段の鎖）。段どうしは互いを知らない
/// </summary>
public interface IGameOverStep
{
    IEnumerator Play(GameOverContext context);
}
