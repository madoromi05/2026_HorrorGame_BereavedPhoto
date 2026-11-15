using UnityEngine;

/// <summary>
/// 段に渡す、今回のゲームオーバーの情報
/// </summary>
public readonly struct GameOverContext
{
    public readonly Transform Player;
    public readonly Transform Enemy;	// 捕まえた敵。デバッグ GUI から起こしたときは null

    public GameOverContext(Transform player, Transform enemy)
    {
        Player = player;
        Enemy  = enemy;
    }
}
