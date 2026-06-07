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
}
