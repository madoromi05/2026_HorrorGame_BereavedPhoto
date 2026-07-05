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
    /// 敵のどの高さを注視点とするか（NavMeshAgent ルートからのオフセット）
    [SerializeField] private float _enemyAimHeight = 1.5f;

    private bool _triggered;
    private PlayerMover _playerMover;
    private PlayerCamera _playerCamera;
    private Transform _mainCameraTransform;

    private void Awake()
    {
        _playerMover = GetComponent<PlayerMover>();
        _playerCamera = GetComponent<PlayerCamera>();
        if (Camera.main != null)
            _mainCameraTransform = Camera.main.transform;
    }

    /// <summary>
    /// ゲームオーバーをトリガーする。
    /// 敵の Transform を渡すと、まず敵の真正面を向いてから遷移する。
    /// </summary>
    public void TriggerGameOver(Transform enemy = null, Transform aimPoint = null)
    {
        if (_triggered) return;
        _triggered = true;

        AudioManager.Instance?.StopBgm(0.5f);
        AudioManager.Instance?.PlaySe(SeType.GameOver);
        enabled = false;

        StartCoroutine(GameOverSequence(enemy, aimPoint));
    }

    private IEnumerator GameOverSequence(Transform enemy, Transform aimPoint)
    {
        if (_playerMover != null) _playerMover.enabled = false;
        if (_playerCamera != null) _playerCamera.enabled = false;   // 入力を即座に遮断（ResetPitch は呼ばない）

        foreach (var e in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            e.Stop();

        if (enemy != null)
        {
            // ── Yaw: 体を敵の方向へ水平回転 ──
            float startYaw = transform.eulerAngles.y;
            Vector3 toEnemy = enemy.position - transform.position;
            toEnemy.y = 0f;
            float targetYaw = toEnemy.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(toEnemy).eulerAngles.y
                : startYaw;

            const float debugDrawDuration = 30f; // 停止(Time.timeScale=0)後もSceneビューで確認できるよう長めに表示

            Debug.Log($"[GameOverHandler] Enemy={enemy.name}, EnemyPos={enemy.position}, PlayerPos={transform.position}, startYaw={startYaw}, targetYaw={targetYaw}, turnAmount={Mathf.DeltaAngle(startYaw, targetYaw)}");
            Debug.DrawLine(transform.position, enemy.position, Color.red, debugDrawDuration, false);
            Debug.DrawRay(transform.position, Quaternion.Euler(0f, startYaw, 0f) * Vector3.forward * 3f, Color.blue, debugDrawDuration, false);
            Debug.DrawRay(transform.position, Quaternion.Euler(0f, targetYaw, 0f) * Vector3.forward * 3f, Color.green, debugDrawDuration, false);

            // ── Pitch: カメラを敵の注視点へ垂直回転 ──
            var camTransform = _mainCameraTransform;
            float startPitch = 0f;
            float targetPitch = 0f;
            if (camTransform != null)
            {
                // localEulerAngles.x は [0, 360] で返るため [-180, 180] に正規化
                float rawPitch = camTransform.localEulerAngles.x;
                startPitch = rawPitch > 180f ? rawPitch - 360f : rawPitch;

                Vector3 aimTarget = aimPoint != null
                    ? aimPoint.position
                    : enemy.position + Vector3.up * _enemyAimHeight;

                // camTransform は Player 直下の子なので、Yaw 回転後にカメラが実際に移動する位置を
                // 先に予測してからピッチを計算する（回転前の位置で計算すると、体の回転量が
                // 大きいほどカメラのオフセット分だけ狙点がズレる）。
                Vector3 futureCamPos = transform.position + Quaternion.Euler(0f, targetYaw, 0f) * camTransform.localPosition;
                Vector3 toAim = aimTarget - futureCamPos;
                float hDist = new Vector2(toAim.x, toAim.z).magnitude;
                // ローカル X 正 = 下向き（Unity FPS 慣例）なので符号反転
                targetPitch = Mathf.Clamp(-Mathf.Atan2(toAim.y, hDist) * Mathf.Rad2Deg, -90f, 90f);

                Debug.Log($"[GameOverHandler] AimTarget={aimTarget}, CameraAngle(before)={camTransform.eulerAngles}, targetYaw={targetYaw}, targetPitch={targetPitch}");
            }

            float elapsed = 0f;
            while (elapsed < _faceTurnDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _faceTurnDuration);
                transform.eulerAngles = new Vector3(0f, Mathf.LerpAngle(startYaw, targetYaw, t), 0f);
                if (camTransform != null)
                    camTransform.localEulerAngles = new Vector3(Mathf.LerpAngle(startPitch, targetPitch, t), 0f, 0f);
                yield return null;
            }
            transform.eulerAngles = new Vector3(0f, targetYaw, 0f);
            if (camTransform != null)
                camTransform.localEulerAngles = new Vector3(targetPitch, 0f, 0f);

            Debug.Log($"[GameOverHandler] CameraAngle(after)={camTransform?.eulerAngles}, PlayerAngle(after)={transform.eulerAngles}");
            Debug.DrawRay(transform.position, transform.forward * 3f, Color.magenta, debugDrawDuration, false);
            if (camTransform != null)
                Debug.DrawRay(camTransform.position, camTransform.forward * 3f, Color.cyan, debugDrawDuration, false);
        }

        yield return new WaitForSeconds(_holdDuration);

        // ここで何かに上書きされていないか最終確認（停止直前のライブ値）
        if (enemy != null)
        {
            Debug.Log($"[GameOverHandler] [停止直前] PlayerForward={transform.forward}, PlayerEulerY={transform.eulerAngles.y}, CamForward={_mainCameraTransform?.forward}, CamEulerX={_mainCameraTransform?.eulerAngles.x}, EnemyPos(now)={enemy.position}");
            Debug.DrawRay(transform.position, transform.forward * 5f, Color.yellow, 30f, false);
            if (_mainCameraTransform != null)
                Debug.DrawRay(_mainCameraTransform.position, _mainCameraTransform.forward * 5f, new Color(1f, 0.5f, 0f), 30f, false);
        }

        Time.timeScale = 0;
        // SceneManager.LoadScene(GameProgressManager.SceneGameOver);
    }
}
