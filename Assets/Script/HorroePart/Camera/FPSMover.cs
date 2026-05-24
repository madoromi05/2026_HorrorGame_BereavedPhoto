using UnityEngine;

/// <summary>
/// キャラクターの水平移動とY軸回転を担当する。
/// CharacterControllerを使用するためコリジョンが有効。
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(InputPlayerController))]
public class FPSMover : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _yawSensitivity = 0.1f;


    private const float kAimSpeed = 0.5f;
    private bool _isAiming = false;

    private Vector2 _moveInput;
    private float _currentYaw;

    private CharacterController _characterController;
    private InputPlayerController _inputCallbackController;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _inputCallbackController = GetComponent<InputPlayerController>();
        _currentYaw = transform.eulerAngles.y;
    }

    private void OnEnable()
    {
        _inputCallbackController.OnMovePerformed += HandleMove;
        _inputCallbackController.OnLookPerformed += HandleLook;
        _inputCallbackController.OnCameraPerformed += HandleCamera;
    }

    private void OnDisable()
    {
        _inputCallbackController.OnMovePerformed -= HandleMove;
        _inputCallbackController.OnLookPerformed -= HandleLook;
        _inputCallbackController.OnCameraPerformed -= HandleCamera;
    }

    private void HandleMove(Vector2 input)
    {
        _moveInput = input;
    }

    private void HandleLook(Vector2 input)
    {
        // 左右回転（Yaw）のみ更新。上下回転はFPSCameraが担当
        _currentYaw += input.x * _yawSensitivity;
        transform.eulerAngles = new Vector3(0f, _currentYaw, 0f);
    }

    private void HandleCamera(bool isAiming)
    {
            _isAiming = isAiming;
    }

    private void FixedUpdate()
    {
        // カメラの上下向きに引きずられないよう水平成分のみ使用
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

        float _speed = 0f;
        if (_isAiming)
        {
                _speed = _moveSpeed * kAimSpeed;
        }else
        {
                _speed = _moveSpeed;
        }

        Vector3 movement = (forward * _moveInput.y + right * _moveInput.x)
             * _speed * Time.fixedDeltaTime;
        _characterController.Move(movement);
    }
}