using UnityEngine;

/// <summary>
/// プレイヤーを Start ルームの初期位置へ移動させる
/// Start ルーム Prefab 内の PlayerSpawnPoint マーカーの位置・向きをそのまま使用する
/// SectionPlacer が持ち、Start ルームを置いた直後に呼ぶ。部屋を置くことはしない
/// </summary>
public class PlayerStartPositioner
{
    private readonly Transform _player;

    public PlayerStartPositioner(Transform player) => _player = player;

    /// <summary>
    /// マーカーの位置・向きへプレイヤーを動かし、プレイヤーを返す
    /// マーカーが無い場合はスポーン位置を決められないため、警告のみ出して現在位置を維持する（部屋中央などへのフォールバックは行わない）
    /// </summary>
    public Transform MoveToStart(GameObject startRoomInstance)
    {
        var marker = startRoomInstance != null
            ? startRoomInstance.GetComponentInChildren<PlayerSpawnPoint>(true)
            : null;

        if (marker == null)
        {
            DebugCustom.LogWarning(
                "SectionPlacer: StartルームにPlayerSpawnPointマーカーが見つかりません。" +
                "StartルームのPrefabにPlayerSpawnPointを配置してください。" +
                "プレイヤーの位置は変更しません。");
            return _player;
        }

        if (_player.TryGetComponent<CharacterController>(out var cc))
            cc.enabled = false;

        _player.position = marker.Position;

        // PlayerMover は内部Yawを基準に回転を制御するため、直接rotationを書くだけでは
        // 最初の視点入力で元に戻ってしまう。SetYaw経由で内部状態も同期させる
        if (_player.TryGetComponent<PlayerMover>(out var mover))
            mover.SetYaw(marker.Yaw);
        else
            _player.rotation = Quaternion.Euler(0f, marker.Yaw, 0f);

        if (cc != null)
            cc.enabled = true;

        return _player;
    }
}
