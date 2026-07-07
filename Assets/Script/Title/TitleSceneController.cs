using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TitleScene にアタッチするコントローラー。
/// ニューゲーム / コンティニュー / アルバム開閉・オプション開閉を管理する。
/// </summary>
public class TitleSceneController : MonoBehaviour
{
    [Header("アルバム")]
    [SerializeField] private AlbumController _albumController;

    [Header("オプション")]
    [SerializeField] private OptionMenuController _optionController;

    [Header("入力")]
    [SerializeField] private InputTitleController _inputTitleController;

    private void OnEnable()
    {
        if (_inputTitleController != null)
            _inputTitleController.OnMenuPerformed += ToggleOption;
    }

    private void OnDisable()
    {
        if (_inputTitleController != null)
            _inputTitleController.OnMenuPerformed -= ToggleOption;
    }

    private void Start()
    {
        AudioManager.Instance?.PlayBgm(BgmType.Title);
    }

    // ---- メニューボタン ----

    public void OnNewGame()
    {
        var mgr = GameProgressManager.Instance;
        if (mgr == null) return;
        // 進行をリセットし、ステージを Horror に設定してホラーシーンへ遷移する。
        mgr.StartHorror();
    }

    public void OnContinue()
    {
        var mgr = GameProgressManager.Instance;
        if (mgr == null) return;
        string scene = mgr.GetSceneForStage(mgr.CurrentStage);
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
    }

    public void OnAlbum()
    {
        _albumController?.OpenAlbum();
    }

    /// UI ボタンからも呼べる直接オープン。
    public void OnOption()
    {
        _optionController?.OpenOption();
    }

    public void OnQuit()
    {
        Application.Quit();
    }

    // ---- 入力イベント ----

    /// キー入力でオプションを開閉トグルする。
    private void ToggleOption()
    {
        if (_optionController == null) return;

        if (_optionController.IsOpen)
            _optionController.CloseOption();
        else
            _optionController.OpenOption();
    }
}
