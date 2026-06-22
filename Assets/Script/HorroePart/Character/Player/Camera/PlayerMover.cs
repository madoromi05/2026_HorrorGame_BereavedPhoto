using UnityEngine;

/// <summary>
/// キャラクターの水平移動とY軸回転を制御する。
/// CharacterController を使用するためコリジョンが有効。
/// 移動速度はしゃがみ・歩き・ダッシュの 3 段階のみ（MoveState で管理）。
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(InputPlayerController))]
[RequireComponent(typeof(PlayerCrouch))]
public class PlayerMover : MonoBehaviour
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

    public MoveState CurrentMoveState => GetMoveState();
    public bool IsDashing { get; set; }
    public bool Frozen { get; set; }

    [Header("移動速度")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _crouchSpeed = 2f;
    [SerializeField] private float _dashSpeed = 8f;

    [Header("感度、調整用設定")]
    [SerializeField] private float _yawSensitivity = 0.1f;
    [SerializeField] private float _aimSensitivityMultiplier = 0.5f;
    [SerializeField] private float _aimSpeedMultiplier = 0.6f;


    [Header("足音")]
    [SerializeField] private float _walkStepInterval = 0.5f;
    [SerializeField] private float _dashStepInterval = 0.3f;
    [SerializeField] private float _dashStepPitch = 1.4f;

    private CharacterController _characterController;
    private InputPlayerController _inputCallbackController;
    private PlayerCrouch _playerCrouch;
    private Vector2 _moveInput;
    private float _currentYaw;
    private float _footStepTimer;
    private bool _isAiming;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _inputCallbackController = GetComponent<InputPlayerController>();
        _playerCrouch = GetComponent<PlayerCrouch>();
        _currentYaw = transform.eulerAngles.y;

        // 保存済み感度を読み込む
        if (PlayerPrefs.HasKey(OptionMenuController.KeySens))
            _yawSensitivity = PlayerPrefs.GetFloat(OptionMenuController.KeySens);
    }

    // オプションメニューからマウス感度を即時反映する。
    public void SetSensitivity(float v) => _yawSensitivity = v;

    private void OnEnable()
    {
        _inputCallbackController.OnMovePerformed   += HandleMove;
        _inputCallbackController.OnLookPerformed   += HandleLook;
        _inputCallbackController.OnCameraPerformed += HandleAiming;
    }

    private void OnDisable()
    {
        _inputCallbackController.OnMovePerformed   -= HandleMove;
        _inputCallbackController.OnLookPerformed   -= HandleLook;
        _inputCallbackController.OnCameraPerformed -= HandleAiming;
        _isAiming = false;
    }

    private void HandleAiming(bool isAiming) => _isAiming = isAiming;

    private void HandleMove(Vector2 input) => _moveInput = input;

    private void HandleLook(Vector2 input)
    {
        if (Frozen) return;
        float sens = _yawSensitivity * (_isAiming ? _aimSensitivityMultiplier : 1f);
        // 左右回転（Yaw）のみ更新。上下回転は FPSCamera が制御
        _currentYaw += input.x * sens;
        transform.eulerAngles = new Vector3(0f, _currentYaw, 0f);
    }

    private void FixedUpdate()
    {
        if (Frozen) return;
        var state = GetMoveState();
        float speed = state switch
        {
            MoveState.Dash => _dashSpeed,
            MoveState.Crouch => _crouchSpeed,
            _ => _moveSpeed,
        };
        if (_isAiming) speed *= _aimSpeedMultiplier;


        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

        Vector2 input = (IsDashing && _moveInput.sqrMagnitude < 0.01f)
            ? Vector2.zero
            : _moveInput;

        Vector3 movement = (forward * input.y + right * input.x) * speed * Time.fixedDeltaTime;
        _characterController.Move(movement);
        UpdateFootStep(state);
    }

    /// <summary>
    /// 現在の入力・フラグから MoveState を判定する。
    /// 優先順位: Dash > Crouch > Walk > Idle
    /// </summary>
    private MoveState GetMoveState()
    {
        if (IsDashing) return MoveState.Dash;
        if (_playerCrouch.IsCrouching) return MoveState.Crouch;
        if (_moveInput.sqrMagnitude > 0.01f) return MoveState.Walk;
        return MoveState.Idle;
    }

    /// <summary>
    /// Playerの移動状態に応じて足音を再生する。歩きとダッシュで間隔とピッチが変わる。
    /// </summary>
    private void UpdateFootStep(MoveState state)
    {
        if (state != MoveState.Walk && state != MoveState.Dash)
        {
            _footStepTimer = 0f;
            return;
        }

        _footStepTimer -= Time.fixedDeltaTime;
        if (_footStepTimer > 0f) return;

        float pitch = state == MoveState.Dash ? _dashStepPitch : 1f;
        float interval = state == MoveState.Dash ? _dashStepInterval : _walkStepInterval;
        AudioManager.Instance?.PlaySe(SeType.FootStep, 1f, pitch);
        _footStepTimer = interval;
    }
}
