/// <summary>
/// DungeonGenerator.OnRoomPlaced を受け取り、
/// スポーン済みの全 EnemyController にプレイヤー参照を渡す仲介コンポーネント。
/// EnemySpawner がプレイヤーを知る必要をなくすため、Player はタグで動的検索する。
/// </summary>
using UnityEngine;

[RequireComponent(typeof(DungeonGenerator))]
public class EnemyPlayerLinker : MonoBehaviour
{
    [SerializeField] GameObject _playerMover;
    private void Awake()
    {
        GetComponent<DungeonGenerator>().OnRoomPlaced += _ => LinkAll();
    }

    private void LinkAll()
    {
        if (_playerMover == null)
        {
            DebugCustom.LogError("EnemyPlayerLinker: Player が見つかりません", this);
            return;
        }

        var player = _playerMover.transform;
        foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            enemy.SetPlayer(player);
    }
}
