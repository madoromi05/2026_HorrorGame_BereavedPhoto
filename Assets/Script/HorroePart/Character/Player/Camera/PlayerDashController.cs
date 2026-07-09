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
    [SerializeField] private float _dashConsumeRate = 20f;
    [SerializeField] private float _dashRegenRate = 20f;
    [SerializeField] private float _dashRegenDelay = 1.5f;

    [Header("疲労（息切れ）設定")]
    // ゲージを使い切ったあと、ダッシュできなくなる秒数
    [SerializeField] private float _exhaustDuration = 5f;

    private PlayerMover _mover;
    private InputPlayerController _inputController;

    private float _dashGauge;
    private float _regenDelayTimer;
    private bool _isSprintKeyHeld;
    private Vector2 _moveInput;

    private bool _isExhausted;
    private float _exhaustTimer;

    // ゲージ枯渇による息切れ中か。PlayerStealthStatus がノイズ増に参照する
    public bool IsExhausted => _isExhausted;

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
        // 疲労タイマーの消化（疲労中はダッシュ不可）
        if (_isExhausted)
        {
            _exhaustTimer -= Time.fixedDeltaTime;
            if (_exhaustTimer <= 0f)
                _isExhausted = false;
        }

        // ダッシュ実行条件: キーを押している ∧ ゲージが残っている ∧ 移動入力がある ∧ 疲労していない
        bool isMoving = _moveInput.sqrMagnitude > 0.01f;
        bool canDash = _isSprintKeyHeld && _dashGauge > 0f && isMoving && !_isExhausted;

        if (canDash)
        {
            _mover.IsDashing = true;
            _dashGauge -= _dashConsumeRate * Time.fixedDeltaTime;
            _regenDelayTimer = _dashRegenDelay;

            if (_dashGauge <= 0f)
            {
                _dashGauge = 0f;
                // ゲージ枯渇 → 一定時間ダッシュ不可＆息切れ（ノイズ増）の疲労状態へ
                _isExhausted = true;
                _exhaustTimer = _exhaustDuration;
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
