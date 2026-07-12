using UnityEngine;
using System.Collections;
using HorrorGame.UI;

/// <summary>
/// スタートルームの出口に配置するトリガー。
/// プレイヤーが通過した瞬間に全敵の追跡を開始する。
/// BoxCollider(isTrigger=true) と共にアタッチして使用する。
/// </summary>
[RequireComponent(typeof(Collider))]
public class StartRoomBoundary : MonoBehaviour
{
    [SerializeField] private GameObject _enemyRevealUI;         // 一瞬表示する敵画像のUI
    [SerializeField] private SeType _enemyRevealSe = SeType.StartRoomExit;
    [SerializeField] private float _freezeDuration = 2f;
    [SerializeField] private float _revealDuration = 1f;        // 画像を出す長さ
    private bool _triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (other.gameObject.layer != LayerMask.NameToLayer("Player")) return;
        _triggered = true;

        // スタートルームを出た時、チュートリアル用の操作UIが有効なら非表示にしてから処理を開始する。
        OperationTutorialUI.HideIfActive();

        StartCoroutine(RevealAndActivate(other.GetComponent<PlayerMover>()));
    }

    private IEnumerator RevealAndActivate(PlayerMover mover)
    {
        // Player を止める
        if (mover != null) mover.Frozen = true;

        // 敵の画像・SE を一瞬出す
        if (_enemyRevealUI != null) _enemyRevealUI.SetActive(true);
        AudioManager.Instance?.PlaySe(_enemyRevealSe);

        yield return new WaitForSeconds(_revealDuration);
        if (_enemyRevealUI != null) _enemyRevealUI.SetActive(false);

        yield return new WaitForSeconds(_freezeDuration - _revealDuration);

        // Player を再開し、敵を起動
        if (mover != null) mover.Frozen = false;
        ActivateAllEnemies();
        gameObject.SetActive(false);
    }

    private void ActivateAllEnemies()
    {
        var enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        DebugCustom.Log($"[StartRoomBoundary] Activate 呼び出し。対象敵数={enemies.Length}", this);
        foreach (var e in enemies)
            e.Activate();

        // このトリガーは実行時生成されるため UsuallyVignette を事前アサインできない。
        // シーン常駐の Volume リグ側を実行時に検索し、通常プレイ用ビネットをフェードインさせる。
        FindFirstObjectByType<UsuallyVignette>()?.Activate();

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
