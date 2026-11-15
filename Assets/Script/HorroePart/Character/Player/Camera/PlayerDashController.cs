using UnityEngine;

/// <summary>
/// ダッシュの入力とゲージ管理を行う
/// キーを押している間だけダッシュ状態にする
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

    public bool IsExhausted => _isExhausted;	// ゲージ枯渇による息切れ中か。PlayerStealthStatus がノイズ増に参照する

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

    private void HandleSprint(bool isPressed) => _isSprintKeyHeld = isPressed;	// キーが押されている間は true、離されたら false が渡される想定

    private void FixedUpdate()
    {
        TickExhaust();

        _mover.IsDashing = CanDash;
        if (CanDash) ConsumeGauge();
        else         RegenerateGauge();
    }

    // キーを押している ∧ ゲージが残っている ∧ 移動入力がある ∧ 疲労していない
    private bool CanDash => _isSprintKeyHeld && _dashGauge > 0f && !_isExhausted
                         && _moveInput.sqrMagnitude > PlayerMover.kMoveInputDeadZoneSqr;

    private void TickExhaust()	// 疲労中はタイマーを減らし、0 で解ける
    {
        if (!_isExhausted) return;

        _exhaustTimer -= Time.fixedDeltaTime;
        if (_exhaustTimer <= 0f)
            _isExhausted = false;
    }

    private void ConsumeGauge()	// ゲージを減らし、使い切ったら疲労にする
    {
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

    private void RegenerateGauge()	// 待ち時間の後でゲージを戻す
    {
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
