using UnityEngine;

/// <summary>
/// HorrorScene にアタッチし、HorrorStageConfig から RoomDataBase を DungeonGenerator に注入する。
/// GameProgressManager のランタイム状態に依存しないため、エディタから直接再生しても正常に動作する。
/// ホラーシーンは1つに統合されており、Inspector で単一の HorrorStageConfig を設定する。
///
/// Unity の実行順序:
///   全 MonoBehaviour の Awake() → 全 MonoBehaviour の Start()
/// このため、Awake() で注入すれば DungeonGenerator.Start() → Generate() より必ず先に実行される。
/// </summary>
public class HorrorSceneConfigurator : MonoBehaviour
{
    [SerializeField] private HorrorStageConfig _stageConfig;
    [SerializeField] private DungeonGenerator  _dungeonGenerator;

    private void Awake()
    {
        if (_stageConfig == null)
        {
            DebugCustom.LogWarning("[HorrorSceneConfigurator] StageConfig が未設定です。", this);
            return;
        }
        if (_dungeonGenerator == null)
        {
            DebugCustom.LogWarning("[HorrorSceneConfigurator] DungeonGenerator が未アサインです。", this);
            return;
        }

        if (_stageConfig.RoomDataBase != null)
            _dungeonGenerator.SetRoomDataBase(_stageConfig.RoomDataBase);

        DebugCustom.Log($"[HorrorSceneConfigurator] 設定: {_stageConfig.Label}", this);
    }

    private void Start()
    {
        AudioManager.Instance?.PlayBgm(BgmType.GameNormal);
    }
}
