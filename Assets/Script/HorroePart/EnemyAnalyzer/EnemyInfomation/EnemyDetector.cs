using UnityEngine;

/// <summary>
/// カメラ前方へ SphereCast を飛ばし、IAnalyzable を持つ敵を検知する。
/// 検知結果は EnemyAnalyzer へ通知する。
/// 検知範囲・距離は Inspector から調整可能。
/// </summary>
public class EnemyDetector : MonoBehaviour
{
    [SerializeField] private EnemyAnalyzer analyzer;
    [SerializeField] private Camera fpsCam;

    [Header("検知設定")]
    [SerializeField] private float detectRange = 2.0f;   // SphereCastの球半径
    [SerializeField] private float detectDistance = 30.0f;  // 最大検知距離
    [SerializeField] private LayerMask enemyLayer;          // 敵レイヤーのみ対象

    private bool _isAiming = false;

    public void SetAiming(bool isAiming) => _isAiming = isAiming;

    private void Update()
    {
        if (!_isAiming)
        {
            analyzer.SetEnemyInRange(false, null);
            return;
        }

        if (TryDetectEnemy(out RaycastHit hit))
        {
            var analyzable = hit.collider.GetComponent<IAnalyzable>();
            analyzer.SetEnemyInRange(true, analyzable);
        }
        else
        {
            analyzer.SetEnemyInRange(false, null);
        }
    }

    /// <summary>
    /// カメラ前方へ SphereCast を飛ばし、敵を検知する。
    /// out引数でヒット情報を返すため、呼び出し側で位置情報を使いたい場合に対応できる。
    /// </summary>
    private bool TryDetectEnemy(out RaycastHit hit)
    {
        Ray ray = new Ray(fpsCam.transform.position, fpsCam.transform.forward);
        return Physics.SphereCast(
            ray,
            detectRange,
            out hit,
            detectDistance,
            enemyLayer
        );
    }

    // デバッグ用：Scene View で検知範囲を可視化
    private void OnDrawGizmosSelected()
    {
        if (fpsCam == null) return;

        Gizmos.color = Color.cyan;
        Vector3 origin = fpsCam.transform.position;
        Vector3 end = origin + fpsCam.transform.forward * detectDistance;

        Gizmos.DrawWireSphere(origin, detectRange);
        Gizmos.DrawLine(origin, end);
        Gizmos.DrawWireSphere(end, detectRange);
    }
}