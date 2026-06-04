using UnityEngine;

/// <summary>
/// オプションパネルの開閉と FPS 設定を担うコントローラー。
/// AlbumController と同様に TitleSceneController から呼び出される。
/// FPS 設定は揮発性（PlayerPrefs への保存なし）で、起動時は 60fps をデフォルトとする。
/// </summary>
public class OptionController : MonoBehaviour
{
    // 選択可能な FPS の選択肢
    private const int kFps30 = 30;
    private const int kFps60 = 60;

    [Header("オプションパネル")]
    [SerializeField] private GameObject _optionPanel;

    private void Start()
    {
        // 起動時は必ずパネルを非表示にし、FPS を既定値に設定する
        _optionPanel.SetActive(false);
        ApplyFrameRate(kFps60);
    }

    public void OpenOption()
    {
        _optionPanel.SetActive(true);
    }

    public void CloseOption()
    {
        _optionPanel.SetActive(false);
    }

    // ボタンの OnClick に登録する
    public void OnSelect30()
    {
        ApplyFrameRate(kFps30);
    }

    public void OnSelect60()
    {
        ApplyFrameRate(kFps60);
    }

    // vSyncCount が 1 以上の場合 targetFrameRate は無視されるため、
    // 確実に制限を反映させるために 0 へ強制する。
    private void ApplyFrameRate(int fps)
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = fps;
    }
}