using UnityEngine;

/// <summary>
/// ランダムダンジョン生成なしで敵を動作させるための初期化コンポーネント。
/// テストシーン等で EnemySpawner の代替として使用する。
///
/// 使い方:
///   1. 敵プレハブ（またはシーン上の敵オブジェクト）にアタッチ
///   2. Inspector で Player を割り当てる
///   3. RoomWanderer を使う場合は UseRoomBounds を有効にして部屋の範囲を設定
///      （無効の場合は WanderFree モードで壁に当たりながら自由徘徊）
/// </summary>
public class StandaloneEnemyInitializer : MonoBehaviour
{
    [SerializeField] private Transform _player;

    [Header("部屋の範囲 (RoomWanderer 用)")]
    [Tooltip("有効にすると RoomWanderer に指定範囲を渡す。無効なら自由徘徊モード。")]
    [SerializeField] private bool _useRoomBounds = false;
    [SerializeField] private Vector3 _roomCenter = Vector3.zero;
    [SerializeField] private Vector3 _roomSize   = new Vector3(10f, 4f, 10f);

    private void Start()
    {
        if (_player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                _player = playerObj.transform;
            else
                DebugCustom.LogWarning("[StandaloneEnemyInitializer] Playerが見つかりません。Inspector で Player を設定してください。");
        }

        if (TryGetComponent<EnemyController>(out var controller))
            controller.SetPlayer(_player);

        if (_useRoomBounds && TryGetComponent<RoomWanderer>(out var roomWanderer))
        {
            var bounds = new Bounds(_roomCenter, _roomSize);
            roomWanderer.SetRoomBounds(bounds);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!_useRoomBounds) return;

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
        Gizmos.DrawCube(_roomCenter, _roomSize);
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
        Gizmos.DrawWireCube(_roomCenter, _roomSize);
    }
}
