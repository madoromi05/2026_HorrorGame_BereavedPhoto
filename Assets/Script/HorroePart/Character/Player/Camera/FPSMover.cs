using UnityEngine;

/// <summary>
/// キャラクターの水平移動とY軸回転を制御する。
/// CharacterController を使用するためコリジョンが有効。
/// 移動速度はしゃがみ・歩き・ダッシュの 3 段階のみ（MoveState で管理）。
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(InputPlayerController))]
public class FPSMover : MonoBehaviour
{
    /// <summary>
    /// 移動状態の列挙型。速度の選択と PlayerStealthStatus の参照に使う。
    /// </summary>
    public enum MoveState
    {
        Idle,   // 静止
        Walk,   // 通常歩き
        Crouch, // しゃがみ歩き
        Dash,   // ダッシュ
    }

    [Header("移動速度")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _crouchSpeed = 2f;
    [SerializeField] private float _yawSensitivity = 0.1f;
    [SerializeField] private float _crouchHeightOffset = 0.7f;
    [SerializeField] private float _dashSpeed = 8f;
    [SerializeField] private float _dashDuration = 2.0f;
    [SerializeField] private float _dashMaxGauge = 100f;
    [SerializeField] private float _dashCostPerUse = 1f;
    [SerializeField] private float _dashRegenRate = 20f;
    [SerializeField] private float _dashRegenDelay = 1.5f;
    [SerializeField] private Transform _cameraTransform;

    public MoveState CurrentMoveState => GetMoveState();

    // ---- プライベートフィールド ----
    private CharacterController _characterController;
    private InputPlayerController _inputCallbackController;
    private Vector2 _moveInput;
    private float _currentYaw;
    private bool _isCrouching;
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
        _inputCallbackController.OnMovePerformed   += HandleMove;
        _inputCallbackController.OnLookPerformed   += HandleLook;
        _inputCallbackController.OnCrouchPerformed += HandleCrouch;
        _inputCallbackController.OnSprintPerformed += HandleSprint;
    }

    private void OnDisable()
    {
        _inputCallbackController.OnMovePerformed   -= HandleMove;
        _inputCallbackController.OnLookPerformed   -= HandleLook;
        _inputCallbackController.OnCrouchPerformed -= HandleCrouch;
        _inputCallbackController.OnSprintPerformed -= HandleSprint;
    }

    private void HandleMove(Vector2 input)  => _moveInput = input;

    private void HandleLook(Vector2 input)
    {
        // 左右回転（Yaw）のみ更新。上下回転は FPSCamera が制御
        _currentYaw += input.x * _yawSensitivity;
        transform.eulerAngles = new Vector3(0f, _currentYaw, 0f);
    }

    private void HandleSprint(bool pressed)
    {
        if (pressed && !_isDashing && _dashGauge >= _dashCostPerUse)
            _sprintPressed = true;
    }

    private void FixedUpdate()
    {
        // ---- ダッシュ状態管理 ----
        if (_sprintPressed)
        {
            _sprintPressed   = false;
            _isDashing       = true;
            _dashTimer       = _dashDuration;
            _dashGauge      -= _dashCostPerUse;
            _regenDelayTimer = _dashRegenDelay;
        }
        if (_isDashing)
        {
            _dashTimer -= Time.fixedDeltaTime;
            if (_dashTimer <= 0f) _isDashing = false;
        }

        // ---- ゲージ回復 ----
        if (_regenDelayTimer > 0f)
            _regenDelayTimer -= Time.fixedDeltaTime;
        else if (_dashGauge < _dashMaxGauge)
            _dashGauge = Mathf.Min(_dashGauge + _dashRegenRate * Time.fixedDeltaTime, _dashMaxGauge);

        // ---- 速度決定（しゃがみ・歩き・ダッシュ の 3 段階のみ） ----
        float speed = GetMoveState() switch
        {
            MoveState.Dash   => _dashSpeed,
            MoveState.Crouch => _crouchSpeed,
            _                => _moveSpeed,
        };

        // ---- 移動ベクトル計算 ----
        // カメラの上下向きに影響されないよう水平成分のみ使用
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 right   = Vector3.ProjectOnPlane(transform.right,   Vector3.up).normalized;

        // その場ダッシュ防止：入力ゼロでダッシュしたときは正面方向へ
        Vector2 input = (_isDashing && _moveInput.sqrMagnitude < 0.01f)
            ? Vector2.up
            : _moveInput;

        Vector3 movement = (forward * input.y + right * input.x) * speed * Time.fixedDeltaTime;
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

    /// <summary>
    /// 現在の入力・フラグから MoveState を判定する。
    /// 優先順位: Dash > Crouch > Walk > Idle
    /// </summary>
    private MoveState GetMoveState()
    {
        if (_isDashing)                      return MoveState.Dash;
        if (_isCrouching)                    return MoveState.Crouch;
        if (_moveInput.sqrMagnitude > 0.01f) return MoveState.Walk;
        return MoveState.Idle;
    }
}
