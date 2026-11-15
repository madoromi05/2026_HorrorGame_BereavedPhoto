using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームオーバーの窓口（Facade）。Player にアタッチし、EnemyController から TriggerGameOver() を呼ぶ
/// プレイヤーと敵を止め、演出の段（IGameOverStep）を順に流し、最後にタイトルへ移る
/// 段ごとの調整値はここの Inspector に置き、起こすたびに段へ渡す
/// </summary>
public class GameOverHandler : MonoBehaviour
{
    public static bool DebugInvincible;	// デバッグ用：true の間はゲームオーバーにならない（無敵）。GameDebugGUI から切り替える

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

        // BGM 停止＋ゲームオーバー SE 再生＋以降の SE 抑止を一括で行う
        AudioManager.Instance?.EnterGameOver(SeType.GameOver);
        GameOverScreen.HideAllUI();
        enabled = false;

        StartCoroutine(GameOverSequence(new GameOverContext(transform, enemy)));
    }

    private IEnumerator GameOverSequence(GameOverContext context)
    {
        FreezePlayerAndEnemies();

        foreach (var step in BuildSteps())
            yield return step.Play(context);

        SceneManager.LoadScene(GameProgressManager.SceneTitleName);
    }

    private void FreezePlayerAndEnemies()
    {
        if (_playerMover != null) _playerMover.enabled = false;
        if (_playerCamera != null) _playerCamera.enabled = false;

        foreach (var e in EnemyRegistry.All)
            e.Stop();
    }

    private IGameOverStep[] BuildSteps() => new IGameOverStep[]	// 演出の順番はここだけで決まる
    {
        new JumpscareStep(_mainCameraTransform, _jumpscareDuration, _jumpscareStartDistance, _jumpscareDistance, _jumpscareHeightOffset),
        new WaitStep(_holdDuration),
        new SignalLossStep(_signalLossDuration, _noiseHoldDuration, _noisePitch),
        new BlackoutStep(_darkenDuration, _blackHoldDuration),
    };
}