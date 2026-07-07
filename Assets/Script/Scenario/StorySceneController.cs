using UnityEngine;

/// <summary>
/// ScenarioPart シーンにアタッチする。
/// GameProgressManager の現在ステージに応じたストーリーデータを StoryManager へ渡し、
/// 全ストーリー完了後に次シーンへ遷移する。
/// </summary>
[RequireComponent(typeof(StoryManager))]
public class StorySceneController : MonoBehaviour
{
    [Header("ストーリーデータ")]
    [Tooltip("Epilogue（クリア）で再生するエンディングストーリー")]
    [SerializeField] private StoryData[] _epilogueStories;

    private StoryManager _storyManager;
    private AsyncOperation _preloadOp;

    private void Awake()
    {
        _storyManager = GetComponent<StoryManager>();

        // Awake で設定することで StoryManager.Start() より確実に先に実行される。
        // Unity の実行順序: 全 Awake() → 全 Start()
        // フローが Title → Horror → Epilogue に統合されたため、
        // ScenarioPart で再生するのはエンディング（クリア）ストーリーのみ。
        _storyManager.SetStoryDatas(_epilogueStories);
        AudioManager.Instance?.PlayBgm(BgmType.Scenario);
    }

    private void Start()
    {
        _storyManager.OnAllStoriesComplete += OnAllComplete;

        // ストーリー再生中に次シーンをバックグラウンドでプリロードしておく
        if (GameProgressManager.Instance != null)
            _preloadOp = GameProgressManager.Instance.PreloadNextScene();
    }

    private void OnDestroy()
    {
        _storyManager.OnAllStoriesComplete -= OnAllComplete;
    }

    private void OnAllComplete()
    {
        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.ActivatePreloadedScene(_preloadOp);
        else
            DebugCustom.LogWarning("[StorySceneController] GameProgressManager not found");
    }
}
