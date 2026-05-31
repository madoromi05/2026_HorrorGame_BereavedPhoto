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
        Prologue,   // 1: ストーリー（ホラー1前）
        Prep1,      // 2: 準備パート（ホラー1前・母の家族情報）
        Horror1,    // 3: ホラーパート1（母の幽霊）
        Interlude,  // 4: ストーリー（ホラー1後）
        Prep2,      // 5: 準備パート（ホラー2前・父の家族情報）
        Horror2,    // 6: ホラーパート2（父の幽霊）
        Epilogue,   // 7: エンディングストーリー
    }

    // シーン名定数（Build Settings に登録必須）
    public const string SceneTitleName  = "TitleScene";
    public const string SceneStoryName  = "ScenarioPart";
    public const string ScenePrepName   = "PrepScene";
    public const string SceneHorrorName = "HorrorScene";
    public const string SceneGameOver   = "GameOverScene";

    // 後方互換用エイリアス（GameDebugGUI 等から参照される定数名を変えないため残す）
    public const string SceneHorror1Name = SceneHorrorName;
    public const string SceneHorror2Name = SceneHorrorName;

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

    public void UnlockAlbumPage(int index)
    {
        if (index < 0 || index >= AlbumPageCount) return;
        AlbumPages[index] = true;
    }

    // ---- シーン遷移 ----

    /// <summary>指定ステージに対応するシーン名を返す。</summary>
    public string GetSceneForStage(GameStage stage) => stage switch
    {
        GameStage.Title      => SceneTitleName,
        GameStage.Prologue   => SceneStoryName,
        GameStage.Prep1      => ScenePrepName,
        GameStage.Horror1    => SceneHorrorName,  // 同じシーン・ステージで中身を切り替える
        GameStage.Interlude  => SceneStoryName,
        GameStage.Prep2      => ScenePrepName,
        GameStage.Horror2    => SceneHorrorName,  // 同じシーン・ステージで中身を切り替える
        GameStage.Epilogue   => SceneStoryName,
        _                    => SceneTitleName,
    };

    /// <summary>
    /// 現在ステージを完了させ、次のステージへ遷移する。
    /// ホラーパート完了時にアルバムページを解放する。
    /// </summary>
    public void LoadNextScene()
    {
        // アルバムページ解放（各ステージの完了タイミング）
        switch (CurrentStage)
        {
            case GameStage.Prologue:  UnlockAlbumPage(0); break; // 表紙
            case GameStage.Horror1:   UnlockAlbumPage(1); break; // 母のページ
            case GameStage.Horror2:   UnlockAlbumPage(2); break; // 父のページ
            case GameStage.Epilogue:  UnlockAlbumPage(3); break; // 終章ページ
        }

        // エピローグ完了 → タイトルへ戻る
        if (CurrentStage == GameStage.Epilogue)
        {
            CurrentStage = GameStage.Title;
            SceneManager.LoadScene(SceneTitleName);
            return;
        }

        AdvanceStage();
        SceneManager.LoadScene(GetSceneForStage(CurrentStage));
    }

    /// <summary>デバッグ用：ステージを直接指定して遷移する。</summary>
    public void DebugJumpToStage(GameStage stage)
    {
        CurrentStage = stage;
        SceneManager.LoadScene(GetSceneForStage(stage));
    }
}
