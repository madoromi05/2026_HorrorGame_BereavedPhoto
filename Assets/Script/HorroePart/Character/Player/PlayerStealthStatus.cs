using UnityEngine;

public class PlayerStealthStatus : MonoBehaviour
{
    [SerializeField] private float walkNoiseRadius = 5f;
    [SerializeField] private float crouchNoiseRadius = 1.5f;

    private FPSMover mover;
    private HandLightController lightController;

    public float FootstepNoiseRadius { get; private set; }
    public bool IsLightOn => lightController.IsLightOn;
    private void Awake()
    {
        mover = GetComponent<FPSMover>();
        lightController = GetComponent<HandLightController>();
    }

    private void Update()
    {
        if (!mover.IsMoving)
            FootstepNoiseRadius = 0f;
        else if (mover.IsCrouching)
            FootstepNoiseRadius = crouchNoiseRadius;
        else
            FootstepNoiseRadius = walkNoiseRadius;
    }
}