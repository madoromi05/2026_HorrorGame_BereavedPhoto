using UnityEngine;

/// <summary>
/// FPSƒJƒƒ‰‚Ìã‰º‰ñ“]‚Ì‚İ‚ğ’S“–‚·‚éB
/// Y²‰ñ“]‚ÍFPSMover‚ª’S“–‚·‚éB
/// </summary>
[RequireComponent(typeof(InputPlayerController))]
public class FPSCamera : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float pitchSensitivity = 0.1f;
    [SerializeField] private float minPitch = -90f;
    [SerializeField] private float maxPitch = 90f;

    private float currentPitch;
    private InputPlayerController inputCallbackController;

    private void Awake()
    {
        inputCallbackController = GetComponent<InputPlayerController>();
    }

    private void OnEnable()
    {
        inputCallbackController.OnLookPerformed += HandleLook;
    }

    private void OnDisable()
    {
        inputCallbackController.OnLookPerformed -= HandleLook;
    }

    private void HandleLook(Vector2 _input)
    {
        // ã‰º‰ñ“]iPitchj‚Ì‚İXVBY²‰ñ“]‚ÍFPSMover‚ª’S“–
        currentPitch -= _input.y * pitchSensitivity;
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        cameraTransform.localEulerAngles = new Vector3(currentPitch, 0f, 0f);
    }
}