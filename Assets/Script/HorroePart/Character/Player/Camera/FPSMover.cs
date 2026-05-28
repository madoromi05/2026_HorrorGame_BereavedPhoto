using UnityEngine;

/// <summary>
/// キャラクターの水平移動とY軸回転を担当する。
/// CharacterControllerを使用するためコリジョンが有効。
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(InputPlayerController))]
public class FPSMover : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float yawSensitivity = 0.1f;
    [SerializeField] private float crouchHeightOffset = 0.7f;

    public bool IsCrouching => isCrouching;
    public bool IsMoving => moveInput.sqrMagnitude > 0.01f;

    private CharacterController characterController;
    private InputPlayerController inputCallbackController;
    private const float kAimSpeed = 0.5f;
    private bool isAiming = false;
    private Vector2 moveInput;
    private float currentYaw;
    private bool isCrouching = false;
    private float standHeight;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        inputCallbackController = GetComponent<InputPlayerController>();
        standHeight = characterController.height;
        currentYaw = transform.eulerAngles.y;
    }

    private void OnEnable()
    {
        inputCallbackController.OnMovePerformed += HandleMove;
        inputCallbackController.OnLookPerformed += HandleLook;
        inputCallbackController.OnCameraPerformed += HandleCamera;
        inputCallbackController.OnCrouchPerformed += HandleCrouch;
    }

    private void OnDisable()
    {
        inputCallbackController.OnMovePerformed -= HandleMove;
        inputCallbackController.OnLookPerformed -= HandleLook;
        inputCallbackController.OnCameraPerformed -= HandleCamera;
        inputCallbackController.OnCrouchPerformed += HandleCrouch;
    }

    private void HandleMove(Vector2 _input)
    {
        moveInput = _input;
    }

    private void HandleLook(Vector2 _input)
    {
        // 左右回転（Yaw）のみ更新。上下回転はFPSCameraが担当
        currentYaw += _input.x * yawSensitivity;
        transform.eulerAngles = new Vector3(0f, currentYaw, 0f);
    }

    private void HandleCamera(bool _isAiming)
    {
            _isAiming = isAiming;
    }

    private void FixedUpdate()
    {
        // カメラの上下向きに引きずられないよう水平成分のみ使用
        Vector3 _forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 _right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

        float _speed = 0f;
        if (isAiming)
        {
                _speed = moveSpeed * kAimSpeed;
        }else
        {
                _speed = moveSpeed;
        }

        Vector3 _movement = (_forward * moveInput.y + _right * moveInput.x)
             * _speed * Time.fixedDeltaTime;
        characterController.Move(_movement);
    }

    private void HandleCrouch(bool _isCrouching)
    {
        isCrouching = _isCrouching;
        float _targetHeight = isCrouching ? standHeight - crouchHeightOffset : standHeight;
        characterController.height = _targetHeight;
        characterController.center = new Vector3(0, _targetHeight / 2f, 0);
    }
}