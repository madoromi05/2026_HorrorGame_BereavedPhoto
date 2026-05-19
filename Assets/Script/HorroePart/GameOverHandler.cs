using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームオーバー条件を受け取りScene遷移を行うコンポーネント。
/// Playerにアタッチし、EnemyControllerから TriggerGameOver() を呼ぶ。
/// </summary>
public class GameOverHandler : MonoBehaviour
{
    private const string kGameOverSceneName = "GameOverScene";

    /// <summary>
    /// ゲームオーバーをトリガーする。
    /// 二重呼び出しを防ぐため、呼び出し後は自身を無効化する。
    /// </summary>
    public void TriggerGameOver()
    {
        enabled = false;
        SceneManager.LoadScene(kGameOverSceneName);
    }
}