using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームオーバー時の演出とタイトル遷移を担うコンポーネント。
/// Player にアタッチし、EnemyController から TriggerGameOver() を呼ぶ。
/// </summary>
public class GameOverHandler : MonoBehaviour
{
    // デバッグ用：true の間はゲームオーバーにならない（無敵）。GameDebugGUI から切り替える。
    public static bool DebugInvincible;

    [SerializeField] private float _holdDuration = 1.0f;

    [Header("ジャンプスケア")]
    [Tooltip("敵が自機に近づききるまでの時間")]
    [SerializeField] private float _jumpscareDuration = 0.12f;

    [Tooltip("捕獲判定")]
    [SerializeField] private float _jumpscareStartDistance = 4.0f;

    [Tooltip("最終的な敵との距離")]
    [SerializeField] private float _jumpscareDistance = 2.5f;

    [Tooltip("敵オブジェクトの高さ")]
    [SerializeField] private float _jumpscareHeightOffset = -0.2f;

    [Header("砂嵐")]
    [Tooltip("砂嵐表示までの時間")]
    [SerializeField] private float _signalLossDuration = 0.8f;
    [Tooltip("砂嵐表示時間")]
    [SerializeField] private float _noiseHoldDuration = 3.0f;
    [Tooltip("ホワイトノイズの再生速度（ピッチ）。1で等速、0.8で遅く低く。")]
    [SerializeField] private float _noisePitch = 0.8f;

    [Header("テレビOFF（黒フェード）")]
    [Tooltip("黒く覆いきってタイトルへ遷移するまでの時間（秒）。この間に OFF 音を聞かせる。")]
    [SerializeField] private float _darkenDuration = 1.0f;
    [Tooltip("黒く覆いきった後、タイトルへ遷移するまで黒画面を保持する時間（秒）。")]
    [SerializeField] private float _blackHoldDuration = 0.5f;

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

    public void TriggerGameOver(Transform enemy = null, Transform aimPoint = null)
    {
        if (DebugInvincible) return;   // デバッグ無敵中はゲームオーバーにしない
        if (_triggered) return;
        _triggered = true;

        // BGM 停止＋ゲームオーバー SE 再生＋以降の SE 抑止を一括で行う。
        AudioManager.Instance?.EnterGameOver(SeType.GameOver);
        DisableAllUI();
        enabled = false;

        StartCoroutine(GameOverSequence(enemy, aimPoint));
    }

    // ゲームオーバー確定時に画面上の全 UI を消す。UI は単一の親にまとまっていないため、
    // シーン内の全ルート Canvas を無効化する。ジャンプスケア（3D）と VHS ノイズ（描画エフェクト）は
    // Canvas ではないため残る。
    private void DisableAllUI()
    {
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (canvas.isRootCanvas) canvas.enabled = false;
    }

    private IEnumerator GameOverSequence(Transform enemy, Transform aimPoint)
    {
        if (_playerMover != null) _playerMover.enabled = false;
        if (_playerCamera != null) _playerCamera.enabled = false;

        foreach (var e in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            e.Stop();

        if (enemy != null)
        {
            Vector3 toEnemy = enemy.position - transform.position;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude > 0.001f)
            {
                float targetYaw = Quaternion.LookRotation(toEnemy).eulerAngles.y;
                transform.eulerAngles = new Vector3(0f, targetYaw, 0f);
            }

            var camTransform = _mainCameraTransform;

            //ジャンプスケア
            if (camTransform != null)
            {
                camTransform.localEulerAngles = new Vector3(0f, 0f, 0f);

                // カメラの正面方向
                Vector3 camForward = camTransform.forward;
                camForward.y = 0f;
                camForward.Normalize();

                //敵の捕獲判定
                Vector3 startPos = camTransform.position + camForward * _jumpscareStartDistance;
                startPos.y = camTransform.position.y + _jumpscareHeightOffset;
                enemy.position = startPos;

                // 最終的に止まる目標位置
                Vector3 targetPos = camCamForwardTarget(camTransform);

                float jumpElapsed = 0f;
                while (jumpElapsed < _jumpscareDuration)
                {
                    jumpElapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(jumpElapsed / _jumpscareDuration);
                    float tCurve = t * t; // 後半加速

                    enemy.position = Vector3.Lerp(startPos, targetPos, tCurve);

                    Vector3 lookDir = camTransform.position - enemy.position;
                    lookDir.y = 0f;
                    if (lookDir.sqrMagnitude > 0.001f)
                    {
                        enemy.rotation = Quaternion.LookRotation(lookDir);
                    }

                    yield return null;
                }
                enemy.position = targetPos;
            }
        }

        yield return new WaitForSeconds(_holdDuration);

        yield return SignalLossOut();

        SceneManager.LoadScene(GameProgressManager.SceneTitleName);
    }

    /// <summary>
    /// カメラの水平正面かつ目の前の、敵が移動すべきターゲット位置を計算する
    /// </summary>
    private Vector3 camCamForwardTarget(Transform camTransform)
    {
        Vector3 camPos = camTransform.position;

        // カメラの正面方向
        Vector3 camForward = camTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        float finalDistance = _jumpscareDistance;

        // 最低限カメラから 0.3m 前方には置くように制限
        if (finalDistance < 0.3f)
        {
            finalDistance = 0.3f;
        }

        Vector3 targetPos = camPos + camForward * finalDistance;

        targetPos.y = camPos.y + _jumpscareHeightOffset;

        return targetPos;
    }

    private IEnumerator SignalLossOut()
    {
        AudioManager.Instance?.StartGameOverNoise(_noisePitch);   // ホワイトノイズ開始（速度 _noisePitch）

        float elapsed = 0f;
        while (elapsed < _signalLossDuration)
        {
            elapsed += Time.deltaTime;
            VhsFeature.SetSignalLoss(elapsed / _signalLossDuration);
            yield return null;
        }
        VhsFeature.SetSignalLoss(1f);

        yield return new WaitForSeconds(_noiseHoldDuration);   // 砂嵐＋ノイズ保持

        AudioManager.Instance?.StopGameOverNoise();               // ノイズをブツッと停止
        AudioManager.Instance?.PlayGameOverSe(SeType.TvPowerOff); // テレビOFF音
        yield return FadeToBlack();                               // 黒フェード（OFF音の余韻も兼ねる）
        yield return new WaitForSeconds(_blackHoldDuration);      // 黒画面を保持してからタイトルへ
    }

    // 画面を黒く覆いきる。黒オーバーレイは DisableAllUI の後にここで生成するため、
    // 無効化の影響を受けず確実に最前面へ出る（GameClearDirector の alpha 補間と同方針）。
    private IEnumerator FadeToBlack()
    {
        var overlay = CreateDarkenOverlay();

        float elapsed = 0f;
        while (elapsed < _darkenDuration)
        {
            elapsed += Time.deltaTime;
            overlay.alpha = Mathf.Clamp01(elapsed / _darkenDuration);
            yield return null;
        }
        overlay.alpha = 1f;
    }

    // 全画面の黒フェード用オーバーレイを最前面 Canvas として実行時生成する。
    // シーン遷移で自動破棄されるため後始末は不要。
    private CanvasGroup CreateDarkenOverlay()
    {
        var go = new GameObject("GameOverDarkenOverlay");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;   // 他の全 UI より手前に描画する

        var group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;                        // 透明から開始
        group.blocksRaycasts = true;             // フェード中の入力を遮断する

        var image = go.AddComponent<Image>();
        image.color = Color.black;               // 不透明な黒（全画面）
        return group;
    }
}