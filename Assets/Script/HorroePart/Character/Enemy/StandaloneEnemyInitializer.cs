using UnityEngine;

/// <summary>
/// ランダムダンジョン生成なしで敵を動作させるための初期化コンポーネント。
/// テストシーン等で EnemySpawner の代替として使用する。
///
/// 使い方:
///   1. 敵プレハブ（またはシーン上の敵オブジェクト）にアタッチ
///   2. Inspector で Player を割り当てる（未設定時は "Player" タグで自動検索）
/// </summary>
public class StandaloneEnemyInitializer : MonoBehaviour
{
    [SerializeField] private Transform _player;

    private void Start()
    {
        if (_player == null)
        {
           DebugCustom.LogWarning("[StandaloneEnemyInitializer] Playerが見つかりません。Inspector で Player を設定してください。");
        }

        if (TryGetComponent<EnemyController>(out var controller))
            controller.SetPlayer(_player);
    }
}
