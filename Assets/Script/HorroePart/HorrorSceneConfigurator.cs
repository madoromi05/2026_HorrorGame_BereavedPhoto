using UnityEngine;

/// <summary>
/// HorrorScene にアタッチし、GameProgressManager のステージに応じて
/// DungeonGenerator の RoomDataBase を起動前に差し替える。
///
/// Unity の実行順序:
///   全 MonoBehaviour の Awake() → 全 MonoBehaviour の Start()
/// このため、Awake() で設定を注入すれば DungeonGenerator.Start() → Generate() より必ず先に実行される。
/// </summary>
public class HorrorSceneConfigurator : MonoBehaviour
{
    [System.Serializable]
    public class HorrorConfig
    {
        [Tooltip("このステージで使用する RoomDataBase（出現させる敵プレハブを含む）")]
        public RoomDataBase RoomDataBase;
        [Tooltip("デバッグ用ステージ名（ログに表示）")]
        public string Label;
    }

    [Header("Horror1 設定（母の幽霊）")]
    [SerializeField] private HorrorConfig _horror1Config;

    [Header("Horror2 設定（父の幽霊）")]
    [SerializeField] private HorrorConfig _horror2Config;
    [SerializeField] private DungeonGenerator _dungeonGenerator;

    private void Awake()
    {
        var stage = GameProgressManager.Instance?.CurrentStage
                    ?? GameProgressManager.GameStage.Horror1;

        bool isHorror1 = stage == GameProgressManager.GameStage.Horror1;
        var  config    = isHorror1 ? _horror1Config : _horror2Config;

        if (config == null)
        {
            DebugCustom.LogWarning($"[HorrorSceneConfigurator] ステージ {stage} の設定が Inspector に未設定です。");
            return;
        }

        if (_dungeonGenerator == null)
        {
            DebugCustom.LogWarning("[HorrorSceneConfigurator] DungeonGenerator が未アサインです。");
            return;
        }

        if (config.RoomDataBase != null)
            _dungeonGenerator.SetRoomDataBase(config.RoomDataBase);

        DebugCustom.Log($"[HorrorSceneConfigurator] ステージ: {stage}  設定: {config.Label}");
    }
}
