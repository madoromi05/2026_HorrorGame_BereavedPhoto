using System.Collections.Generic;

/// <summary>
/// シーンにいる敵（EnemyController）の一覧を持つ係
/// EnemyController が有効になると入り、無効になると抜ける。敵をシーン全体から探し直さずに済ませるために使う
/// </summary>
public static class EnemyRegistry
{
    private static readonly List<EnemyController> _enemies = new List<EnemyController>();

    public static IReadOnlyList<EnemyController> All => _enemies;

    public static void Register(EnemyController enemy)   => _enemies.Add(enemy);
    public static void Unregister(EnemyController enemy) => _enemies.Remove(enemy);

    /// <summary>
    /// 指定した種類の敵を返す。呼んだ側が退場させても一覧が崩れないよう、写しのリストで返す
    /// </summary>
    public static List<EnemyController> OfType(EnemyType type)
    {
        var result = new List<EnemyController>();
        foreach (var enemy in _enemies)
        {
            if (enemy.TryGetComponent<GhostIdentity>(out var ghost) && ghost.GhostType == type)
                result.Add(enemy);
        }
        return result;
    }
}
