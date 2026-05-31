using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームオーバーを受け取り GameOverScene へ遷移するコンポーネント。
/// Player にアタッチし、EnemyController から TriggerGameOver() を呼ぶ。
/// GameProgressManager のステージは変更しない（リトライで同じシーンに戻れるようにするため）。
/// </summary>
public class GameOverHandler : MonoBehaviour
{
    /// <summary>
    /// ゲームオーバーをトリガーする。
    /// 二重呼び出しを防ぐため、呼び出し後は自身を無効化する。
    /// </summary>
    public void TriggerGameOver()
    {
        enabled = false;
        SceneManager.LoadScene(GameProgressManager.SceneGameOver);
    }
}
