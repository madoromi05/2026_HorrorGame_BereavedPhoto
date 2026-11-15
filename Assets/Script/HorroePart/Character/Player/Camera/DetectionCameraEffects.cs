using UnityEngine;

/// <summary>
/// 敵にプレイヤーが発見された瞬間と追跡中の画面演出の窓口（Facade）
///
/// 発見時: カメラを一度強く揺らす（BurstShake）
/// 追跡中に移動しているとき: カメラを軽く揺らし続ける（ChaseSway）
/// 通常の歩行: ヘッドボブとライトボブ（BobWave・HandLightBob）
/// 調整値はここの Inspector に置き、毎フレーム各部へ渡す。どの揺れを優先するかはここで決める
/// </summary>
public class DetectionCameraEffects : MonoBehaviour
{
    [SerializeField] private PlayerCamera _playerCamera;

    [Header("発見時の強い揺れ（一回）")]
    [SerializeField] private float _burstDuration   = 0.6f;
    [SerializeField] private float _burstMagnitude  = 5f;   // 最大揺れ角度（度）
    [SerializeField] private float _burstFrequency  = 22f;  // ノイズ周波数（速さ）

    [Header("追跡中の移動揺れ")]
    [SerializeField] private float _walkMagnitude  = 1.2f;  // 揺れ角度（度）
    [SerializeField] private float _walkFrequency  = 7f;

    [Header("通常歩行ヘッドボブ")]
    [SerializeField] private float _bobMagnitude = 0.35f;  // 揺れ角度（度）
    [SerializeField] private float _bobFrequency = 4f;     // 周波数（速さ）

    [Header("ライトボブ")]
    [SerializeField] private HandLightController _handLightController;
    [SerializeField] private GameObject _handLight;
    [SerializeField] private float _lightBobMagnitude = 0.04f;  // ローカル座標（m）

    private PlayerMover  _playerMover;
    private ChaseWatcher _chaseWatcher;
    private BurstShake   _burst;
    private BobWave      _bobWave;
    private HandLightBob _lightBob;

    private void Awake()
    {
        _playerMover = GetComponent<PlayerMover>();

        if (_playerCamera == null)
            _playerCamera = GetComponentInChildren<PlayerCamera>();
        if (_playerCamera == null)
            DebugCustom.LogError("[DetectionCameraEffects] PlayerCamera が見つかりません。", this);

        if (_handLightController == null)
            _handLightController = GetComponent<HandLightController>();

        _lightBob     = new HandLightBob(_handLightController, _handLight);
        _chaseWatcher = new ChaseWatcher();
        _burst        = new BurstShake();
        _bobWave      = new BobWave(_playerMover);
    }

    private void Update()
    {
        bool isChased = _chaseWatcher.Tick(out bool chaseStarted);
        if (chaseStarted) _burst.Play(_burstDuration);	// 追跡開始を検出したらバーストシェイクをトリガー

        bool isMoving = _playerMover != null &&
                        _playerMover.CurrentMoveState != PlayerMover.MoveState.Idle;

        // シェイク計算 → PlayerCamera に注入
        if (_playerCamera != null)
            _playerCamera.SetShakeAngle(ComputeShake(isChased, isMoving));

        ApplyLightBob(isMoving, isChased);
    }

    /// <summary>
    /// 今のカメラの揺れを選んで返す
    /// 優先順：発見時の強い揺れ → 追跡中に移動しているときの揺れ → 通常歩行のヘッドボブ
    /// </summary>
    private Vector3 ComputeShake(bool isChased, bool isMoving)
    {
        if (_burst.IsPlaying)
            return _burst.Tick(_burstDuration, _burstMagnitude, _burstFrequency);

        if (isChased && isMoving)
            return ChaseSway.Compute(_walkMagnitude, _walkFrequency);

        if (isMoving)
        {
            var wave = _bobWave.Sample(_bobMagnitude, _bobFrequency);
            return new Vector3(wave.x, wave.y, 0f);		// 頭は横が大きく縦が小さい
        }

        return Vector3.zero;
    }

    private void ApplyLightBob(bool isMoving, bool isChased)	// 歩いていて追われていないときだけ、ライトを揺らす
    {
        if (!_lightBob.HasTarget) return;

        if (!isMoving || isChased)
        {
            _lightBob.Reset();
            return;
        }

        var wave = _bobWave.Sample(_lightBobMagnitude, _bobFrequency);
        _lightBob.Set(new Vector3(wave.y, wave.x, 0f));	// ライトは縦が大きく横が小さい
    }
}
