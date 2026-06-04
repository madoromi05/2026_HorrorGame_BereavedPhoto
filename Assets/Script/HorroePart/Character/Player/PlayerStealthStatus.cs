using UnityEngine;

public class PlayerStealthStatus : MonoBehaviour
{
    // 足音の届く半径（m）。EnemyDetector の音検知がこの値を参照する。
    // 値が大きいほど遠くの敵にも気づかれやすくなる。
    [Header("足音検知半径")]
    [SerializeField] private float _walkNoiseRadius = 5f;
    [SerializeField] private float _crouchNoiseRadius = 1.5f;
    [SerializeField] private float _dashNoiseRadius = 10f;

    [Header("息切れ（疲労）ノイズ")]
    // ダッシュ疲労中に底上げされる足音半径
    [SerializeField] private float _exhaustNoiseRadius = 7f;

    private PlayerMover _mover;
    private HandLightController _lightController;
    private PlayerBreath _breath;
    private PlayerDashController _dashController;
    private PlayerHidingController _hiding;

    public float FootstepNoiseRadius { get; private set; }
    public bool IsLightOn => _lightController.IsLightOn;
    public bool IsHiding  => _hiding != null && _hiding.IsHiding;

    private void Awake()
    {
        _mover = GetComponent<PlayerMover>();
        _lightController = GetComponent<HandLightController>();
        _breath = GetComponent<PlayerBreath>();
        _dashController = GetComponent<PlayerDashController>();
        _hiding = GetComponent<PlayerHidingController>();
    }

    private void Update()
    {
        // 隠れ中は完全に無音
        if (IsHiding)
        {
            FootstepNoiseRadius = 0f;
            return;
        }

        // MoveState ごとに足音半径を切り替える（PlayerMover.MoveState と対応）
        float noise = _mover.CurrentMoveState switch
        {
            PlayerMover.MoveState.Dash   => _dashNoiseRadius,
            PlayerMover.MoveState.Walk   => _walkNoiseRadius,
            PlayerMover.MoveState.Crouch => _crouchNoiseRadius,
            _                         => 0f,  // Idle：静止中は無音
        };

        if (_breath != null)
        {
            if (_breath.IsGasping)
                noise = Mathf.Max(noise, _breath.GaspNoiseRadius);
            else if (_breath.IsHoldingBreath)
                noise = 0f;
        }

        // ダッシュ疲労（息切れ）中はノイズを底上げ。息止めより優先して上書き
        if (_dashController != null && _dashController.IsExhausted)
            noise = Mathf.Max(noise, _exhaustNoiseRadius);

        FootstepNoiseRadius = noise;
    }
}
