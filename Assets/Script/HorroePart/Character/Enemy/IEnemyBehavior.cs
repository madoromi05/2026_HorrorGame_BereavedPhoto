using UnityEngine;

/// <summary>
/// 敵の徘徊行動を表すインターフェース。
/// RoomWanderer（部屋内徘徊）またはMapWanderer（マップ全体徘徊）をPrefabにアタッチして実装を切り替える。
/// EnemyControllerはこのインターフェース経由でTickを呼び出し、追跡時は呼び出しを停止する。
/// </summary>
public interface IEnemyBehavior
{
    /// <summary>
    /// FixedUpdateのタイミングで呼び出される徘徊処理。
    /// </summary>
    void Tick();
}