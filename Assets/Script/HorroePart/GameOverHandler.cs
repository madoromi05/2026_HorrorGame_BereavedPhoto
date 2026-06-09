using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームオーバーを受け取り GameOverScene へ遷移するコンポーネント。
/// Player にアタッチし、EnemyController から TriggerGameOver() を呼ぶ。
/// GameProgressManager のステージは変更しない（リトライで同じシーンに戻れるようにするため）。
/// </summary>
public class GameOverHandler : MonoBehaviour
{
    [SerializeField] private float _faceTurnDuration = 0.5f;
    [SerializeField] private float _holdDuration = 1.0f;

    private bool _triggered;

    /// <summary>
    /// ゲームオーバーをトリガーする。
    /// 敵の Transform を渡すと、まず敵の方向へ振り向いてから遷移する。
    /// </summary>
    public void TriggerGameOver(Transform enemy = null)
    {
        if (_triggered) return;
        _triggered = true;

        AudioManager.Instance?.StopBgm(0.5f);
        enabled = false;

        StartCoroutine(GameOverSequence(enemy));
    }

    private IEnumerator GameOverSequence(Transform enemy)
    {
        var mover = GetComponent<PlayerMover>();
        var cam   = GetComponent<PlayerCamera>();
        if (mover != null) mover.enabled = false;
        if (cam   != null) cam.enabled   = false;

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (enemy != null)
        {
            float   startYaw  = transform.eulerAngles.y;
            Vector3 toEnemy   = enemy.position - transform.position;
            toEnemy.y = 0f;
            float targetYaw = toEnemy.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(toEnemy).eulerAngles.y
                : startYaw;

            float elapsed = 0f;
            while (elapsed < _faceTurnDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _faceTurnDuration);
                transform.eulerAngles = new Vector3(0f, Mathf.LerpAngle(startYaw, targetYaw, t), 0f);
                yield return null;
            }
            transform.eulerAngles = new Vector3(0f, targetYaw, 0f);
        }

        yield return new WaitForSeconds(_holdDuration);

        SceneManager.LoadScene(GameProgressManager.SceneGameOver);
    }
}
