using UnityEngine;

/// <summary>
/// ゲーム中メニュー（ESC キーで開閉）。
/// OptionMenuController を利用して音声・操作設定を提供する。
/// 開いている間は Time.timeScale=0 でゲームを一時停止し、カーソルを表示する。
/// </summary>
public class GameMenuController : MonoBehaviour
{
    [SerializeField] private OptionMenuController _optionMenu;
    [SerializeField] private InputPlayerController _inputController;

    private bool _isOpen;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void OnEnable()
    {
        if (_inputController != null)
            _inputController.OnMenuPerformed += ToggleMenu;
    }

    private void OnDisable()
    {
        if (_inputController != null)
            _inputController.OnMenuPerformed -= ToggleMenu;
    }

    public void ToggleMenu()
    {
        if (_isOpen) CloseMenu();
        else         OpenMenu();
    }

    public void OpenMenu()
    {
        _isOpen = true;
        _optionMenu?.OpenOption();
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        _inputController?.SetPlayerInputEnabled(false);
    }

    public void CloseMenu()
    {
        _isOpen = false;
        _optionMenu?.CloseOption();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        _inputController?.SetPlayerInputEnabled(true);
    }

    /// <summary>
    /// オプション画面の「タイトルへ戻る」ボタンから呼ぶ。
    /// メニューを開いている間は Time.timeScale=0 で停止しているため、
    /// シーン遷移前に必ず 1 へ戻す（LoadScene は timeScale をリセットしないため、
    /// このリセットを怠るとタイトルシーンが停止状態で読み込まれる）。
    /// カーソルはタイトル側の TitleSceneController.Start で再設定される。
    /// </summary>
    public void OnReturnToTitle()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(GameProgressManager.SceneTitleName);
    }
}
