using UnityEngine;

/// <summary>
/// ダッシュの入力とゲージ管理を行う。
/// キーを押している間だけダッシュ状態にする。
/// </summary>
[RequireComponent(typeof(PlayerMover))]
[RequireComponent(typeof(InputPlayerController))]
public class PlayerDashController : MonoBehaviour
{
    [Header("ダッシュゲージ設定")]
    [SerializeField] private float _dashMaxGauge = 100f;
    [SerializeField] private float _dashConsumeRate = 30f; // 1秒間押し続けた際の消費量
    [SerializeField] private float _dashRegenRate = 20f;
    [SerializeField] private float _dashRegenDelay = 1.5f;

    private PlayerMover _mover;
    private InputPlayerController _inputController;

    private float _dashGauge;
    private float _regenDelayTimer;
    private bool _isSprintKeyHeld;
    private Vector2 _moveInput;

    private void Awake()
    {
        _mover = GetComponent<PlayerMover>();
        _inputController = GetComponent<InputPlayerController>();
        _dashGauge = _dashMaxGauge;
    }

    private void OnEnable()
    {
        _inputController.OnMovePerformed += HandleMove;
        _inputController.OnSprintPerformed += HandleSprint;
    }

    private void OnDisable()
    {
        _inputController.OnMovePerformed -= HandleMove;
        _inputController.OnSprintPerformed -= HandleSprint;
    }

    private void HandleMove(Vector2 input) => _moveInput = input;

    // キーが押されている間は true、離されたら false が渡される想定
    private void HandleSprint(bool isPressed) => _isSprintKeyHeld = isPressed;

    private void FixedUpdate()
    {
        // ダッシュ実行条件: キーを押している ＆ ゲージが残っている ＆ 移動入力がある
        bool isMoving = _moveInput.sqrMagnitude > 0.01f;
        bool canDash = _isSprintKeyHeld && _dashGauge > 0f && isMoving;

        if (canDash)
        {
            _mover.IsDashing = true;
            _dashGauge -= _dashConsumeRate * Time.fixedDeltaTime;
            _regenDelayTimer = _dashRegenDelay;

            if (_dashGauge < 0f)
            {
                _dashGauge = 0f;
            }
        }
        else
        {
            _mover.IsDashing = false;

            // ゲージの回復処理
            if (_regenDelayTimer > 0f)
            {
                _regenDelayTimer -= Time.fixedDeltaTime;
            }
            else if (_dashGauge < _dashMaxGauge)
            {
                _dashGauge = Mathf.Min(_dashGauge + _dashRegenRate * Time.fixedDeltaTime, _dashMaxGauge);
            }
        }
    }
}