using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TitleScene にアタッチするコントローラー。
/// ニューゲーム / コンティニュー / アルバム開閉・オプション開閉を管理する。
/// </summary>
public class TitleSceneController : MonoBehaviour
{
    [Header("オプション")]
    [SerializeField] private OptionMenuController _optionController;

    [Header("入力")]
    [SerializeField] private InputTitleController _inputTitleController;

    [Header("画面演出")]
    [SerializeField] private TitleIntroDirector _introDirector;

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
        // ゲームオーバー／クリア画面はカーソルを固定・非表示にするため、
        // タイトルへ戻った時点で必ずマウス操作を有効化しておく。
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        AudioManager.Instance?.PlayBgm(BgmType.Title);
    }

    // ---- メニューボタン ----

    public void OnNewGame()
    {
        var mgr = GameProgressManager.Instance;
        if (mgr == null) return;

        // 暗転＋ドア音の演出を挟んでからホラーシーンへ遷移する。
        // 演出完了コールバックで StartHorror() を呼ぶことで、暗転を見せてからロードする。
        if (_introDirector != null)
            _introDirector.PlayStartSequence(mgr.StartHorror);
        else
            mgr.StartHorror();
    }

    public void OnContinue()
    {
        var mgr = GameProgressManager.Instance;
        if (mgr == null) return;
        string scene = mgr.GetSceneForStage(mgr.CurrentStage);
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
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
