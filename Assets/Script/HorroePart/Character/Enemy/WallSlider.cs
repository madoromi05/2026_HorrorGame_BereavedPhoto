using UnityEngine;

/// <summary>
/// 壁に接触したときの法線を保持し、移動方向を壁面に沿うよう補正するクラス。
/// 複数の接触点法線を個別に Slerp 補間した上で最大成分合成し、
/// 角（2壁が交わる箇所）で法線平均がゼロになる問題を回避する。
/// OnCollisionExit 直後に OnCollisionStay が再発火するチャタリングを吸収するため、
/// Exit 時は即リセットせず kExitCooldownDuration 秒の猶予を持たせてからリセットする。
/// </summary>
public class WallSlider : MonoBehaviour
{
    // 法線スムージングの追従速度。大きいほど滑らかだが追従が遅くなる
    [SerializeField] private float normalSmoothing = 15f;

    private Vector3 _wallNormal;
    private bool _isTouchingWall;

    private float _wallContactDuration;
    private float _xzSpeed;

    // OnCollisionExit 後に即リセットせず、この秒数だけ isTouchingWall を維持する
    private float _exitCooldown;

    private const float kStuckSpeedThreshold = 0.05f;
    private const float kStuckDurationThreshold = 0.5f;

    // 角・薄い壁で OnCollisionExit → OnCollisionStay が高頻度で交互に来るチャタリングを吸収する猶予時間
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

        // 猶予時間が切れたらリセット。OnCollisionStay が来ていれば Awake で上書きされているため影響なし
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
        // Stay が来たので猶予タイマーをキャンセルして接触状態を維持する
        _exitCooldown = 0f;

        var compositeNormal = CompositeNormal(col);

        // 合成結果がゼロの場合（極めて稀）は前フレームの法線を維持する
        if (compositeNormal == Vector3.zero) return;

        // 前フレームの法線から Slerp 補間して同フレームの急激な反転を抑える
        _wallNormal = Vector3.Slerp(_wallNormal, compositeNormal, normalSmoothing * Time.fixedDeltaTime);
        _isTouchingWall = true;

        _wallContactDuration += Time.fixedDeltaTime;
        _xzSpeed = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z).magnitude;

        if (_wallContactDuration >= kStuckDurationThreshold && _xzSpeed <= kStuckSpeedThreshold)
        {
            DebugCustom.LogWarning(
                $"[WallSlider] 停止検知: {gameObject.name}\n" +
                $"  壁法線(スムージング後) = {_wallNormal}\n" +
                $"  壁法線(合成)          = {compositeNormal}\n" +
                $"  XZ速度               = {_xzSpeed:F4}\n" +
                $"  接触継続時間         = {_wallContactDuration:F2}s\n" +
                $"  接触オブジェクト      = {col.gameObject.name}\n" +
                $"  接触点数             = {col.contactCount}"
            );
        }
    }

    private void OnCollisionExit(Collision col)
    {
        // 即リセットせず猶予時間を設定する。チャタリングで Stay が来たらタイマーはキャンセルされる
        _exitCooldown = kExitCooldownDuration;
    }

    /// <summary>
    /// 接触点の法線を XZ 平面に投影し、各成分の絶対値最大値で合成する。
    /// 単純平均では2壁が直角のとき法線がゼロになるため、
    /// 成分ごとに最大値を採用することで必ず有効な脱出方向を保持する。
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
    /// 壁に接触中であれば法線平面への射影で方向を補正して返す。
    /// 補正後がゼロベクトル（角に真正面から当たっている）の場合は
    /// 合成法線方向（壁から離れる方向）を返して脱出を促す。
    /// </summary>
    public Vector3 SlideDirection(Vector3 direction)
    {
        if (!_isTouchingWall || _wallNormal == Vector3.zero) return direction;

        var slid = Vector3.ProjectOnPlane(direction, _wallNormal).normalized;

        // 角詰まり時は合成法線そのものを脱出方向として返す
        if (slid == Vector3.zero)
            return _wallNormal;

        return slid;
    }

    public bool IsTouchingWall => _isTouchingWall;

    /// <summary>
    /// 角詰まり（壁接触が一定時間続き XZ 速度がほぼゼロ）を検出したら true を返し、
    /// 同時に wallNormal に現在の壁法線を出力して _wallContactDuration をリセットする。
    /// wallNormal を呼び出し側に渡すことで、壁から離れる方向を基準にした脱出方向の抽選を可能にする。
    /// プロパティではなくメソッドにすることで「読んだら消費される」一発イベントとして扱い、
    /// 毎フレーム true を返し続けて Behavior 側で方向が連続再抽選されるのを防ぐ。
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