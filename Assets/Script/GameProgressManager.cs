using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// シーンを跨いだゲーム進行状態を管理するシングルトン（DontDestroyOnLoad）。
/// 本番用ではないため PlayerPrefs は使用せず、実行中のみ状態を保持する。
/// RuntimeInitializeOnLoadMethod で自動生成するため手動配置不要。
/// </summary>
public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    public enum GameStage
    {
        Title,      // 0: タイトル画面
        Horror,     // 1: ホラーパート（単一・母と父の幽霊を解析）
        Epilogue,   // 2: エンディング（クリア）ストーリー
    }

    // シーン名定数（Build Settings に登録必須）
    public const string SceneTitleName  = "TitleScene";
    public const string SceneStoryName  = "ScenarioPart";    // Epilogue（クリア）で使用
    public const string SceneHorrorName = "RandomMapScene";  // 単一のホラーシーン
    public const string SceneGameOver   = "GameOverScene";
    public const string SceneGameClear  = "GameClearScene";  // 全敵解析クリア時の遷移先

    public const int AlbumPageCount = 4;

    // インメモリのみ（実行中リセットされる）
    public GameStage CurrentStage { get; private set; } = GameStage.Title;
    public bool[] AlbumPages { get; private set; } = new bool[AlbumPageCount];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance != null) return;
        var go = new GameObject("[GameProgressManager]");
        go.AddComponent<GameProgressManager>();
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }

    // ---- 進行制御 ----

    /// <summary>ステージを一つ進める。</summary>
    public void AdvanceStage()
    {
        int max = System.Enum.GetValues(typeof(GameStage)).Length - 1;
        CurrentStage = (GameStage)Mathf.Min((int)CurrentStage + 1, max);
    }

    /// <summary>進行を最初からリセットする（ニューゲーム用）。</summary>
    public void ResetProgress()
    {
        CurrentStage = GameStage.Title;
        AlbumPages   = new bool[AlbumPageCount];
    }

    /// <summary>ニューゲーム開始。進行をリセットしてホラーシーンへ遷移する。</summary>
    public void StartHorror()
    {
        ResetProgress();
        CurrentStage = GameStage.Horror;
        SceneManager.LoadScene(SceneHorrorName);
    }

    public void UnlockAlbumPage(int index)
    {
        if (index < 0 || index >= AlbumPageCount) return;
        AlbumPages[index] = true;
    }

    // ---- シーン遷移 ----

    /// <summary>指定ステージに対応するシーン名を返す。</summary>
    public string GetSceneForStage(GameStage stage) => stage switch
    {
        GameStage.Title    => SceneTitleName,
        GameStage.Horror   => SceneHorrorName,
        GameStage.Epilogue => SceneStoryName,
        _                  => SceneTitleName,
    };

    /// <summary>
    /// 現在ステージを完了させ、次のステージへ遷移する。
    /// ホラーパート完了時にアルバムページを解放する。
    /// </summary>
    public void LoadNextScene()
    {
        UnlockCurrentStageAlbumPage();

        if (CurrentStage == GameStage.Epilogue)
        {
            CurrentStage = GameStage.Title;
            SceneManager.LoadScene(SceneTitleName);
            return;
        }

        AdvanceStage();
        SceneManager.LoadScene(GetSceneForStage(CurrentStage));
    }

    /// <summary>
    /// 次シーンをバックグラウンドでプリロードする。allowSceneActivation = false のため
    /// ActivatePreloadedScene() が呼ばれるまでシーンは切り替わらない。
    /// ストーリー・準備パートの開始時に呼び出し、終了時に ActivatePreloadedScene() で切り替える。
    /// </summary>
    public AsyncOperation PreloadNextScene()
    {
        string nextSceneName;
        if (CurrentStage == GameStage.Epilogue)
        {
            nextSceneName = SceneTitleName;
        }
        else
        {
            int max = System.Enum.GetValues(typeof(GameStage)).Length - 1;
            var next = (GameStage)Mathf.Min((int)CurrentStage + 1, max);
            nextSceneName = GetSceneForStage(next);
        }

        var op = SceneManager.LoadSceneAsync(nextSceneName);
        op.allowSceneActivation = false;
        return op;
    }

    /// <summary>
    /// プリロード済みの AsyncOperation を有効化し、ステージを進める。
    /// op が null の場合は通常の LoadNextScene() にフォールバックする。
    /// </summary>
    public void ActivatePreloadedScene(AsyncOperation op)
    {
        UnlockCurrentStageAlbumPage();

        if (CurrentStage == GameStage.Epilogue)
            CurrentStage = GameStage.Title;
        else
            AdvanceStage();

        if (op != null)
            op.allowSceneActivation = true;
        else
            SceneManager.LoadScene(GetSceneForStage(CurrentStage));
    }

    private void UnlockCurrentStageAlbumPage()
    {
        switch (CurrentStage)
        {
            case GameStage.Horror:    // ホラークリアで表紙・母・父のページを解放
                UnlockAlbumPage(0);   // 表紙
                UnlockAlbumPage(1);   // 母のページ
                UnlockAlbumPage(2);   // 父のページ
                break;
            case GameStage.Epilogue:  UnlockAlbumPage(3); break; // 終章ページ
        }
    }
}
