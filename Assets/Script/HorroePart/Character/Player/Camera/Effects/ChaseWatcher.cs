/// <summary>
/// 敵がプレイヤーを追っているかを毎フレーム見て、追跡が始まった瞬間を知らせる
/// DetectionCameraEffects が持ち、Update で 1 回だけ Tick する。揺れや演出は持たない
/// </summary>
public class ChaseWatcher
{
    private bool _wasChased;

    public bool Tick(out bool started)	// 今追われているかを返す。追われ始めたフレームだけ started が true
    {
        bool isChased = IsAnyChasing();
        started = isChased && !_wasChased;
        _wasChased = isChased;
        return isChased;
    }

    private static bool IsAnyChasing()
    {
        var enemies = EnemyRegistry.All;
        for (int i = 0; i < enemies.Count; i++)	// foreach だと毎フレーム列挙子の確保が出る
            if (enemies[i].IsActivated)
                return true;
        return false;
    }
}
