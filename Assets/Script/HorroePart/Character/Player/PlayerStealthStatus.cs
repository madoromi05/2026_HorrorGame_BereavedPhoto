using UnityEngine;

public class PlayerStealthStatus : MonoBehaviour
{
    // 足音の届く半径（m）。EnemyDetector の音検知がこの値を参照する。
    // 値が大きいほど遠くの敵にも気づかれやすくなる。
    [Header("足音検知半径")]
    [SerializeField] private float _walkNoiseRadius = 5f;
    [SerializeField] private float _crouchNoiseRadius = 1.5f;
    [SerializeField] private float _dashNoiseRadius = 10f;

    private FPSMover _mover;
    private HandLightController _lightController;

    public float FootstepNoiseRadius { get; private set; }
    public bool IsLightOn => _lightController.IsLightOn;

    private void Awake()
    {
        _mover = GetComponent<FPSMover>();
        _lightController = GetComponent<HandLightController>();
    }

    private void Update()
    {
        if (_mover.IsDashing)
            FootstepNoiseRadius = _dashNoiseRadius;
        else if (!_mover.IsMoving)
            FootstepNoiseRadius = 0f;
        else if (_mover.IsCrouching)
            FootstepNoiseRadius = _crouchNoiseRadius;
        else
            FootstepNoiseRadius = _walkNoiseRadius;
    }
}
