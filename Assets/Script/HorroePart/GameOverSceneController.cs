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
    /// <summary>現在のホラーシーンを再ロードする。</summary>
    public void OnRetry()
    {
        var mgr = GameProgressManager.Instance;
        if (mgr == null)
        {
            // フォールバック: ステージ不明のためホラー1へ
            SceneManager.LoadScene(GameProgressManager.SceneHorror1Name);
            return;
        }

        // ステージが Horror1/Horror2 以外になっている場合はホラー1へ戻す
        var stage = mgr.CurrentStage;
        bool isHorrorStage = stage == GameProgressManager.GameStage.Horror1
                          || stage == GameProgressManager.GameStage.Horror2;

        string scene = isHorrorStage
            ? mgr.GetSceneForStage(stage)
            : GameProgressManager.SceneHorror1Name;

        SceneManager.LoadScene(scene);
    }

    /// <summary>タイトルへ戻る（ステージは変更しない）。</summary>
    public void OnQuit()
    {
        SceneManager.LoadScene(GameProgressManager.SceneTitleName);
    }
}
