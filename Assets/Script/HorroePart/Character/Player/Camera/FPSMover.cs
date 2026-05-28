using UnityEngine;

/// <summary>
/// キャラクターの水平移動とY軸回転を制御する。
/// CharacterControllerを使用するためコリジョンが有効。
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(InputPlayerController))]
public class FPSMover : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _yawSensitivity = 0.1f;
    [SerializeField] private float _crouchHeightOffset = 0.7f;  // しゃがみで縮む Collider 高さ
    [SerializeField] private float _dashSpeed = 8f;             // ダッシュ速度
    [SerializeField] private float _dashDuration = 2.0f;        // ダッシュの持続時間（秒）
    [SerializeField] private float _dashMaxGauge = 100f;        // ダッシュゲージの最大値
    [SerializeField] private float _dashCostPerUse = 1f;        // ダッシュ使用時のゲージ消費量
    [SerializeField] private float _dashRegenRate = 20f;        // ダッシュゲージの回復速度
    [SerializeField] private float _dashRegenDelay = 1.5f;      // ダッシュ後の回復開始までの遅延時間
    [SerializeField] private Transform _cameraTransform;

    public bool IsCrouching => _isCrouching;
    public bool IsMoving => _moveInput.sqrMagnitude > 0.01f;
    public bool IsDashing => _isDashing;
    public float DashGauge => _dashGauge;
    public float DashMaxGauge => _dashMaxGauge;

    private CharacterController _characterController;
    private InputPlayerController _inputCallbackController;
    private const float kAimSpeed = 0.5f;
    private bool _isAiming = false;
    private Vector2 _moveInput;
    private float _currentYaw;
    private bool _isCrouching = false;
    private float _standHeight;
    private float _cameraStandLocalY;
    private float _dashGauge;
    private bool _isDashing;
    private float _dashTimer;
    private float _regenDelayTimer;
    private bool _sprintPressed;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _inputCallbackController = GetComponent<InputPlayerController>();
        _standHeight = _characterController.height;
        _currentYaw = transform.eulerAngles.y;
        _dashGauge = _dashMaxGauge;
        if (_cameraTransform != null)
            _cameraStandLocalY = _cameraTransform.localPosition.y;
    }

    private void OnEnable()
    {
        _inputCallbackController.OnMovePerformed += HandleMove;
        _inputCallbackController.OnLookPerformed += HandleLook;
        _inputCallbackController.OnCameraPerformed += HandleCamera;
        _inputCallbackController.OnCrouchPerformed += HandleCrouch;
        _inputCallbackController.OnSprintPerformed += HandleSprint;
    }

    private void OnDisable()
    {
        _inputCallbackController.OnMovePerformed -= HandleMove;
        _inputCallbackController.OnLookPerformed -= HandleLook;
        _inputCallbackController.OnCameraPerformed -= HandleCamera;
        _inputCallbackController.OnCrouchPerformed -= HandleCrouch;
        _inputCallbackController.OnSprintPerformed -= HandleSprint;
    }

    private void HandleMove(Vector2 input)
    {
        _moveInput = input;
    }

    private void HandleLook(Vector2 input)
    {
        // 左右回転（Yaw）のみ更新。上下回転はFPSCameraが制御
        _currentYaw += input.x * _yawSensitivity;
        transform.eulerAngles = new Vector3(0f, _currentYaw, 0f);
    }

    private void HandleCamera(bool isAiming)
    {
        _isAiming = isAiming;
    }

    private void HandleSprint(bool pressed)
    {
        if (pressed && !_isDashing && _dashGauge >= _dashCostPerUse)
            _sprintPressed = true;
    }

    private void FixedUpdate()
    {
        // ダッシュ開始
        if (_sprintPressed)
        {
            _sprintPressed = false;
            _isDashing = true;
            _dashTimer = _dashDuration;
            _dashGauge -= _dashCostPerUse;
            _regenDelayTimer = _dashRegenDelay;
        }

        // ダッシュタイマー
        if (_isDashing)
        {
            _dashTimer -= Time.fixedDeltaTime;
            if (_dashTimer <= 0f)
                _isDashing = false;
        }

        // ゲージ回復
        if (_regenDelayTimer > 0f)
            _regenDelayTimer -= Time.fixedDeltaTime;
        else if (_dashGauge < _dashMaxGauge)
            _dashGauge = Mathf.Min(_dashGauge + _dashRegenRate * Time.fixedDeltaTime, _dashMaxGauge);

        // 速度決定
        float speed;
        if (_isDashing) speed = _dashSpeed;
        else if (_isAiming) speed = _moveSpeed * kAimSpeed;
        else speed = _moveSpeed;

        // カメラの上下向きに影響されないよう水平成分のみ使用
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

        Vector2 input = (_isDashing && _moveInput.sqrMagnitude < 0.01f)
        ? Vector2.up
        : _moveInput;

        Vector3 movement = (forward * _moveInput.y + right * _moveInput.x)
             * speed * Time.fixedDeltaTime;
        _characterController.Move(movement);
    }

    private void HandleCrouch(bool isCrouching)
    {
        _isCrouching = isCrouching;
        float targetHeight = _isCrouching ? _standHeight - _crouchHeightOffset : _standHeight;
        _characterController.height = targetHeight;
        _characterController.center = new Vector3(0, targetHeight / 2f, 0);

        if (_cameraTransform != null)
        {
            float cameraY = _isCrouching ? _cameraStandLocalY - _crouchHeightOffset : _cameraStandLocalY;
            Vector3 localPos = _cameraTransform.localPosition;
            _cameraTransform.localPosition = new Vector3(localPos.x, cameraY, localPos.z);
        }
    }
}
