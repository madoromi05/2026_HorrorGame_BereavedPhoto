using UnityEngine;

/// <summary>
/// ScenarioPart シーンにアタッチする。
/// GameProgressManager の現在ステージに応じたストーリーデータを StoryManager へ渡し、
/// 全ストーリー完了後に次シーンへ遷移する。
/// </summary>
[RequireComponent(typeof(StoryManager))]
public class StorySceneController : MonoBehaviour
{
    [Header("ステージ対応ストーリーデータ")]
    [Tooltip("Prologue ステージで再生するストーリー")]
    [SerializeField] private StoryData[] _prologueStories;

    [Tooltip("Interlude ステージで再生するストーリー")]
    [SerializeField] private StoryData[] _interludeStories;

    [Tooltip("Epilogue ステージで再生するストーリー")]
    [SerializeField] private StoryData[] _epilogueStories;

    private StoryManager _storyManager;

    private void Awake()
    {
        _storyManager = GetComponent<StoryManager>();

        // Awake で設定することで StoryManager.Start() より確実に先に実行される。
        // Unity の実行順序: 全 Awake() → 全 Start()
        var stage = GameProgressManager.Instance != null
            ? GameProgressManager.Instance.CurrentStage
            : GameProgressManager.GameStage.Prologue;

        var stories = stage switch
        {
            GameProgressManager.GameStage.Prologue  => _prologueStories,
            GameProgressManager.GameStage.Interlude => _interludeStories,
            GameProgressManager.GameStage.Epilogue  => _epilogueStories,
            _                                       => _prologueStories,
        };

        _storyManager.SetStoryDatas(stories);
    }

    private void Start()
    {
        _storyManager.OnAllStoriesComplete += OnAllComplete;
    }

    private void OnDestroy()
    {
        _storyManager.OnAllStoriesComplete -= OnAllComplete;
    }

    private void OnAllComplete()
    {
        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.LoadNextScene();
        else
            Debug.LogWarning("[StorySceneController] GameProgressManager not found");
    }
}
