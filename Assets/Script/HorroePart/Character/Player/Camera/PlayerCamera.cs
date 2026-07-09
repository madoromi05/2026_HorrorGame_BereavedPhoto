using UnityEngine;

/// <summary>
/// FPSカメラの上下回転のみを制御する。
/// Y軸回転はFPSMoverが制御する。
/// </summary>
[RequireComponent(typeof(InputPlayerController))]
public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _pitchSensitivity = 0.1f;
    [SerializeField] private float _aimSensitivityMultiplier = 0.5f;
    [SerializeField] private float _minPitch = -90f;
    [SerializeField] private float _maxPitch = 90f;

    private float _currentPitch;
    private Vector3 _shakeAngle;
    private bool _isAiming;
    private InputPlayerController _inputCallbackController;
    private PlayerMover _playerMover;
    private float _pendingLookY;

    private void Awake()
    {
        _inputCallbackController = GetComponent<InputPlayerController>();
        _playerMover = GetComponent<PlayerMover>();

        if (_cameraTransform == null)
            DebugCustom.LogError($"[PlayerCamera] _cameraTransform が未設定です。", this);

        // 保存済み感度を読み込む（未保存なら Inspector 値をそのまま使う）
        if (PlayerPrefs.HasKey(OptionMenuController.KeySens))
            _pitchSensitivity = PlayerPrefs.GetFloat(OptionMenuController.KeySens);
    }

    // オプションメニューからマウス感度を即時反映する
    public void SetSensitivity(float v) => _pitchSensitivity = v;

    // DetectionCameraEffects からシェイク角度（度）を毎フレーム注入する
    public void SetShakeAngle(Vector3 angle) => _shakeAngle = angle;

    // ピッチを 0 にリセットしてカメラを水平に戻す
    public void ResetPitch()
    {
        _currentPitch = 0f;
        _shakeAngle   = Vector3.zero;
        if (_cameraTransform != null)
            _cameraTransform.localEulerAngles = Vector3.zero;
    }

    private void OnEnable()
    {
        _inputCallbackController.OnLookPerformed   += HandleLook;
        _inputCallbackController.OnCameraPerformed += HandleAiming;
    }

    private void OnDisable()
    {
        _inputCallbackController.OnLookPerformed   -= HandleLook;
        _inputCallbackController.OnCameraPerformed -= HandleAiming;
        _isAiming = false;
    }

    private void HandleAiming(bool isAiming) => _isAiming = isAiming;

    /// <summary>
    /// Look入力は1フレーム内に複数回発火することがあり、マウスのデルタ値はフレーム内で
    /// 累積されているため、都度加算すると過剰回転になる。ここでは最新値を保持するだけにし、
    /// 実際のPitch適用は Update で1フレームにつき1回だけ行う。
    /// </summary>
    private void HandleLook(Vector2 input) => _pendingLookY = input.y;

    private void Update()
    {
        if (_playerMover != null && _playerMover.Frozen) return;
        if (_pendingLookY == 0f) return;

        float multiplier = _isAiming ? _aimSensitivityMultiplier : 1f;
        float sens = _pitchSensitivity * multiplier;
        // 上下回転（Pitch）のみ更新。Y軸回転はFPSMoverが制御
        _currentPitch -= _pendingLookY * sens;
        _currentPitch = Mathf.Clamp(_currentPitch, _minPitch, _maxPitch);
        _pendingLookY = 0f;
    }

    private void LateUpdate()
    {
        if (_cameraTransform == null) return;
        _cameraTransform.localEulerAngles = new Vector3(_currentPitch + _shakeAngle.x, _shakeAngle.y, 0f);
    }
}
