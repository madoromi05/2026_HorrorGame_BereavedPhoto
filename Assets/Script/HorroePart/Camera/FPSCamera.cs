using UnityEngine;

/// <summary>
/// FPSƒJƒƒ‰‚Ìã‰º‰ñ“]‚Ì‚İ‚ğ’S“–‚·‚éB
/// Y²‰ñ“]‚ÍFPSMover‚ª’S“–‚·‚éB
/// </summary>
[RequireComponent(typeof(InputCallbackController))]
public class FPSCamera : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _pitchSensitivity = 0.1f;
    [SerializeField] private float _minPitch = -90f;
    [SerializeField] private float _maxPitch = 90f;

    private float _currentPitch;
    private InputCallbackController _inputCallbackController;

    private void Awake()
    {
        _inputCallbackController = GetComponent<InputCallbackController>();
    }

    private void OnEnable()
    {
        _inputCallbackController.OnLookPerformed += HandleLook;
    }

    private void OnDisable()
    {
        _inputCallbackController.OnLookPerformed -= HandleLook;
    }

    private void HandleLook(Vector2 input)
    {
        // ã‰º‰ñ“]iPitchj‚Ì‚İXVBY²‰ñ“]‚ÍFPSMover‚ª’S“–
        _currentPitch -= input.y * _pitchSensitivity;
        _currentPitch = Mathf.Clamp(_currentPitch, _minPitch, _maxPitch);
        _cameraTransform.localEulerAngles = new Vector3(_currentPitch, 0f, 0f);
    }
}