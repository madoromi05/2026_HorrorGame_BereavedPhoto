using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TitleScene にアタッチするコントローラー。
/// ニューゲーム / コンティニュー / アルバム開閉を管理する。
/// </summary>
public class TitleSceneController : MonoBehaviour
{
    [Header("アルバム")]
    [SerializeField] private AlbumController _albumController;

    [Header("オプション")]
    [SerializeField] private OptionController _optionController;

    private void Start()
    {
        AudioManager.Instance?.PlayBgm(BgmType.Title);
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

    public void OnOption()
    {
        _optionController?.OpenOption();
    }
}
