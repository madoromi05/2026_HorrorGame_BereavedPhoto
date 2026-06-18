using UnityEngine;

/// <summary>
/// スタートルームの出口に配置するトリガー。
/// プレイヤーが通過した瞬間に全敵の追跡を開始する。
/// BoxCollider(isTrigger=true) と共にアタッチして使用する。
/// </summary>
[RequireComponent(typeof(Collider))]
public class StartRoomBoundary : MonoBehaviour
{
    private bool _triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (other.gameObject.layer != LayerMask.NameToLayer("Player")) return;
        _triggered = true;
        gameObject.SetActive(false);
        ActivateAllEnemies();
    }

    private void ActivateAllEnemies()
    {
        var enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        DebugCustom.Log($"[StartRoomBoundary] Activate 呼び出し。対象敵数={enemies.Length}", this);
        foreach (var e in enemies)
            e.Activate();

        AudioManager.Instance?.PlayBgm(BgmType.GameChase);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.4f);
        if (TryGetComponent<BoxCollider>(out var col))
            Gizmos.DrawCube(transform.position, col.size);
    }
#endif
}
