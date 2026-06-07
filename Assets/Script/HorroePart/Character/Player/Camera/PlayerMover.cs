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

    [Header("移動速度")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _crouchSpeed = 2f;
    [SerializeField] private float _dashSpeed = 8f;

    [Header("感度、調整用設定")]
    [SerializeField] private float _yawSensitivity = 0.1f;

    public MoveState CurrentMoveState => GetMoveState();
    public bool IsDashing { get; set; }

    private CharacterController _characterController;
    private InputPlayerController _inputCallbackController;
    private PlayerCrouch _playerCrouch;
    private Vector2 _moveInput;
    private float _currentYaw;

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

    /// <summary>オプションメニューからマウス感度を即時反映する。</summary>
    public void SetSensitivity(float v) => _yawSensitivity = v;

    private void OnEnable()
    {
        _inputCallbackController.OnMovePerformed += HandleMove;
        _inputCallbackController.OnLookPerformed += HandleLook;
    }

    private void OnDisable()
    {
        _inputCallbackController.OnMovePerformed -= HandleMove;
        _inputCallbackController.OnLookPerformed -= HandleLook;
    }

    private void HandleMove(Vector2 input)  => _moveInput = input;

    private void HandleLook(Vector2 input)
    {
        // 左右回転（Yaw）のみ更新。上下回転は FPSCamera が制御
        _currentYaw += input.x * _yawSensitivity;
        transform.eulerAngles = new Vector3(0f, _currentYaw, 0f);
    }

    private void FixedUpdate()
    {
        float speed = GetMoveState() switch
        {
            MoveState.Dash => _dashSpeed,
            MoveState.Crouch => _crouchSpeed,
            _ => _moveSpeed,
        };

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

        Vector2 input = (IsDashing && _moveInput.sqrMagnitude < 0.01f)
            ? Vector2.zero
            : _moveInput;

        Vector3 movement = (forward * input.y + right * input.x) * speed * Time.fixedDeltaTime;
        _characterController.Move(movement);
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
}
