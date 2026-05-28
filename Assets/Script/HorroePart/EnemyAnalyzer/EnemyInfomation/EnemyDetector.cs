using UnityEngine;

/// <summary>
/// カメラ前方へ SphereCast を飛ばし、IAnalyzable を持つ敵を検知する。
/// 検知結果は EnemyAnalyzer へ通知する。
/// 検知範囲・距離は Inspector から調整可能。
/// </summary>
public class EnemyDetector : MonoBehaviour
{
    [SerializeField] private EnemyAnalyzer _analyzer;
    [SerializeField] private Camera _fpsCam;

    [Header("検知設定")]
    [SerializeField] private float _detectRange = 2.0f;      // SphereCastの球半径
    [SerializeField] private float _detectDistance = 30.0f;  // 最大検知距離
    [SerializeField] private LayerMask _enemyLayer;          // 敵レイヤーのみ対象

    private bool _isAiming = false;

    public void SetAiming(bool isAiming) => _isAiming = isAiming;

    private void Update()
    {
        if (!_isAiming)
        {
            _analyzer.SetEnemyInRange(false);
            return;
        }

        if (TryDetectEnemy(out RaycastHit hit))
        {
            _analyzer.SetEnemyInRange(true);
        }
        else
        {
            _analyzer.SetEnemyInRange(false);
        }
    }

    /// <summary>
    /// カメラ前方へ SphereCast を飛ばし、敵を検知する。
    /// out引数でヒット情報を返すため、呼び出し側で位置情報を使いたい場合に対応できる。
    /// </summary>
    private bool TryDetectEnemy(out RaycastHit hit)
    {
        Ray ray = new Ray(_fpsCam.transform.position, _fpsCam.transform.forward);
        return Physics.SphereCast(
            ray,
            _detectRange,
            out hit,
            _detectDistance,
            _enemyLayer
        );
    }

    // デバッグ用：Scene View で検知範囲を可視化
    private void OnDrawGizmosSelected()
    {
        if (_fpsCam == null) return;

        Gizmos.color = Color.cyan;
        Vector3 origin = _fpsCam.transform.position;
        Vector3 end = origin + _fpsCam.transform.forward * _detectDistance;

        Gizmos.DrawWireSphere(origin, _detectRange);
        Gizmos.DrawLine(origin, end);
        Gizmos.DrawWireSphere(end, _detectRange);
    }
}