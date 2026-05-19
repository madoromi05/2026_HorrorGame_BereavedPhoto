using UnityEngine;

/// <summary>
/// 敵の徘徊行動を表すインターフェース。
/// RoomWanderer（部屋内徘徊）または MapWanderer（マップ全体徘徊）を Prefab にアタッチして使い分ける。
/// EnemyController はこのインターフェース経由で Tick を呼び出し、追跡時は呼び出しを止める。
/// 追跡終了時は OnChaseEnded で通知し、各 Behavior が必要に応じて状態をリセットする。
/// </summary>
public interface IEnemyBehavior
{
    /// <summary>
    /// FixedUpdate のタイミングで呼び出される徘徊処理。
    /// </summary>
    void Tick();

    /// <summary>
    /// EnemyController が追跡を終了した瞬間に一度だけ呼び出される。
    /// 部屋外に出た場合の帰還フラグ立てなど、追跡終了後の状態リセットに使用する。
    /// </summary>
    void OnChaseEnded();
}