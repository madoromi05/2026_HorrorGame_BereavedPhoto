/// <summary>
/// 自キャラの移動状態に合わせて足音を鳴らす係。歩きとダッシュで間隔とピッチが変わる
/// PlayerMover が Awake で作り、FixedUpdate で移動した後に Inspector の値と一緒に Tick を呼ぶ
/// 移動状態を決めるのは PlayerMover。ここは次の足音までの残り時間だけを持ち、調整値は持たない
/// </summary>
public class PlayerFootsteps
{
    private const float kWalkStepPitch = 1f;	// 歩きの足音は素材のままの高さ
    private const float kStepVolume    = 1f;	// 足音は素材のままの音量

    private float _footStepTimer;

    /// <summary>
    /// Playerの移動状態に応じて足音を再生する。歩きとダッシュで間隔とピッチが変わる
    /// </summary>
    public void Tick(PlayerMover.MoveState state, float deltaTime, float walkStepInterval, float dashStepInterval, float dashStepPitch)
    {
        if (state != PlayerMover.MoveState.Walk && state != PlayerMover.MoveState.Dash)
        {
            _footStepTimer = 0f;
            return;
        }

        _footStepTimer -= deltaTime;
        if (_footStepTimer > 0f) return;

        float pitch = state == PlayerMover.MoveState.Dash ? dashStepPitch : kWalkStepPitch;
        float interval = state == PlayerMover.MoveState.Dash ? dashStepInterval : walkStepInterval;
        AudioManager.Instance?.PlaySe(SeType.FootStep, kStepVolume, pitch);
        _footStepTimer = interval;
    }
}
