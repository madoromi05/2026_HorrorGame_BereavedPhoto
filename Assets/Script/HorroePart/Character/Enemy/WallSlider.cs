using UnityEngine;

/// <summary>
/// 壁に接触したときの法線を保持し、移動方向を壁面に合うよう補正するクラス。
/// 複数の接触点の法線を合成して最大値で合算し、最大絶対値で合わせる。
/// 角（2壁の合わせ目）で法線がゼロになる場合に前フレームの法線を保持する。
/// OnCollisionExit の後に OnCollisionStay が発火するチャタリングを想定するため、
/// Exit 時は即座セットせず kExitCooldownDuration 秒の猶予時間が経ってからリセットする。
/// </summary>
public class WallSlider : MonoBehaviour
{
    [SerializeField] private float _normalSmoothing = 15f;

    private Vector3 _wallNormal;
    private bool _isTouchingWall;

    private float _wallContactDuration;
    private float _xzSpeed;

    private float _exitCooldown;

    private const float kStuckSpeedThreshold = 0.05f;
    private const float kStuckDurationThreshold = 0.5f;
    private const float kExitCooldownDuration = 0.1f;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (_exitCooldown <= 0f) return;

        _exitCooldown -= Time.fixedDeltaTime;

        // 猶予時間が切れたらリセット。OnCollisionStay が来ている場合は Awake で上書きされているため影響なし
        if (_exitCooldown <= 0f)
        {
            _isTouchingWall = false;
            _wallNormal = Vector3.zero;
            _wallContactDuration = 0f;
            _xzSpeed = 0f;
        }
    }

    private void OnCollisionStay(Collision col)
    {
        // 敵同士の衝突は壁として扱わない
        if (col.gameObject.GetComponent<EnemyController>() != null) return;

        _exitCooldown = 0f;

        var compositeNormal = CompositeNormal(col);

        if (compositeNormal == Vector3.zero) return;

        _wallNormal = Vector3.Slerp(_wallNormal, compositeNormal, _normalSmoothing * Time.fixedDeltaTime);
        _isTouchingWall = true;

        _wallContactDuration += Time.fixedDeltaTime;
        _xzSpeed = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z).magnitude;

        if (_wallContactDuration >= kStuckDurationThreshold && _xzSpeed <= kStuckSpeedThreshold)
        {
            DebugCustom.LogWarning(
                $"[WallSlider] 停止検知: {gameObject.name}\n" +
                $"  壁法線(スムージング後) = {_wallNormal}\n" +
                $"  壁法線(生値)          = {compositeNormal}\n" +
                $"  XZ速度               = {_xzSpeed:F4}\n" +
                $"  接触継続時間         = {_wallContactDuration:F2}s\n" +
                $"  接触オブジェクト      = {col.gameObject.name}\n" +
                $"  接触点数             = {col.contactCount}"
            );
        }
    }

    private void OnCollisionExit(Collision col)
    {
        // 敵同士の衝突はスキップ
        if (col.gameObject.GetComponent<EnemyController>() != null) return;

        // 即セットせず猶予時間を設定する。
        _exitCooldown = kExitCooldownDuration;
    }

    /// <summary>
    /// 接触点の法線を XZ 方向に投影し、各軸の絶対値で最大値で合成する。
    /// 単軸合算では2壁に挟まったとき法線がゼロになるため、
    /// 各軸ごとに最大値を取る用を採用することで必ずいずれかの壁から出る方向を保持する。
    /// </summary>
    private static Vector3 CompositeNormal(Collision col)
    {
        float maxX = 0f;
        float maxZ = 0f;

        foreach (var contact in col.contacts)
        {
            var n = contact.normal;
            if (Mathf.Abs(n.x) > Mathf.Abs(maxX)) maxX = n.x;
            if (Mathf.Abs(n.z) > Mathf.Abs(maxZ)) maxZ = n.z;
        }

        return new Vector3(maxX, 0f, maxZ).normalized;
    }

    /// <summary>
    /// 壁に接触している場合、法線方向への射影で方向を補正して返す。
    /// 補正後がゼロベクトル（壁に真正面から当たっている）の場合は
    /// 壁法線方向（壁から離れる方向）を返して脱出を促す。
    /// </summary>
    public Vector3 SlideDirection(Vector3 direction)
    {
        if (!_isTouchingWall || _wallNormal == Vector3.zero) return direction;

        var slid = Vector3.ProjectOnPlane(direction, _wallNormal).normalized;

        // 角詰まり時は壁法線そのものを脱出方向として返す
        if (slid == Vector3.zero)
            return _wallNormal;

        return slid;
    }

    public bool IsTouchingWall => _isTouchingWall;

    /// <summary>
    /// 角詰まり（壁接触かつ一定時間 XZ 速度がほぼゼロ）を検出したら true を返し、
    /// wallNormal に現在の壁法線を出力する。同時に _wallContactDuration をリセットする。
    /// wallNormal を呼び出し元に渡すことで、壁から離れる方向に向けた脱出処理を可能にする。
    /// プロパティではなくメソッドにすることで「読んだとき発火」イベントとして扱い、
    /// 同フレーム true を返した後 Behavior 側で処理し、再度検出を防ぐ。
    /// </summary>
    public bool ConsumeStuck(out Vector3 wallNormal)
    {
        wallNormal = _wallNormal;

        if (!_isTouchingWall
            || _wallContactDuration < kStuckDurationThreshold
            || _xzSpeed > kStuckSpeedThreshold)
            return false;

        _wallContactDuration = 0f;
        return true;
    }
}
