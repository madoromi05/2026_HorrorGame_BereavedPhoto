using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TitleScene にアタッチするコントローラー。
/// ニューゲーム / コンティニュー / アルバム開閉を管理する。
/// </summary>
public class TitleSceneController : MonoBehaviour
{
    [Header("コンティニューが有効かの判定")]
    [Tooltip("この Stage 以降をコンティニューとして扱う")]
    [SerializeField] private GameProgressManager.GameStage _continueMinStage
        = GameProgressManager.GameStage.Horror1;

    [Header("アルバム")]
    [SerializeField] private AlbumController _albumController;

    private void Start()
    {
        var mgr = GameProgressManager.Instance;

        bool canContinue = mgr != null && (int)mgr.CurrentStage >= (int)_continueMinStage;
    }

    public void OnNewGame()
    {
        var mgr = GameProgressManager.Instance;
        if (mgr == null) return;
        mgr.ResetProgress();   // Stage = Title
        mgr.LoadNextScene();   // Title → Prologue → ScenarioPart
    }

    public void OnContinue()
    {
        var mgr = GameProgressManager.Instance;
        if (mgr == null) return;
        // 現在の Stage のシーンをそのまま読み込む
        string scene = mgr.GetSceneForStage(mgr.CurrentStage);
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
    }

    public void OnAlbum()
    {
        _albumController?.OpenAlbum();
    }
}
