using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// GameOverScene にアタッチするコントローラー。
/// GameProgressManager のステージは GameOverHandler が変更しないため、
/// リトライ時は同じホラーシーンに戻ることができる。
/// </summary>
public class GameOverSceneController : MonoBehaviour
{
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    /// <summary>ホラーシーンを再ロードする。</summary>
    public void OnRetry()
    {
        // ホラーシーンは1つに統合されたため、常に単一のホラーシーンへ戻る。
        SceneManager.LoadScene(GameProgressManager.SceneHorrorName);
    }

    /// <summary>タイトルへ戻る（ステージは変更しない）。</summary>
    public void OnQuit()
    {
        SceneManager.LoadScene(GameProgressManager.SceneTitleName);
    }
}
