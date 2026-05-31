using UnityEngine;

/// <summary>
/// カメラの正面に向けてSphereCastを飛ばし、敵を検知するクラス。
/// 検知結果はEnemyAnalyzerに通知される。
/// 検知範囲や距離はInspectorから調整可能。
/// </summary>
public class EnemyDetector : MonoBehaviour
{
    [SerializeField] private EnemyAnalyzer _analyzer;
    [SerializeField] private Camera _fpsCam;

    [Header("検知設定")]
    [SerializeField] private float _detectRange = 2.0f;      // SphereCastの半径（検知の当たり判定の太さ）
    [SerializeField] private float _detectDistance = 30.0f;  // 最大検知距離
    [SerializeField] private LayerMask _enemyLayer;          // 検知対象とする敵のレイヤー

    private bool _isAiming = false;

    private void Awake()
    {
        DebugCustom.ValidateFields(this,
            (nameof(_analyzer), _analyzer),
            (nameof(_fpsCam), _fpsCam));
    }

    /// <summary>
    /// エイム状態を設定する。
    /// </summary>
    public void SetAiming(bool isAiming) => _isAiming = isAiming;

    private void Update()
    {
        if (_analyzer == null) return;

        // エイム中でない場合は検知状態をリセットして処理を抜ける
        if (!_isAiming)
        {
            _analyzer.SetCurrentEnemy(null);
            _analyzer.SetEnemyInRange(false);
            return;
        }

        // エイム中の場合は敵の検知を試みる
        if (TryDetectEnemy(out RaycastHit hit))
        {
            // 子コライダーがヒットしても常にルートの EnemyController を使う
            var root = hit.collider.GetComponentInParent<EnemyController>();
            _analyzer.SetCurrentEnemy(root != null ? root.gameObject : hit.collider.gameObject);
            _analyzer.SetEnemyInRange(true);
        }
        else
        {
            _analyzer.SetCurrentEnemy(null);
            _analyzer.SetEnemyInRange(false);
        }
    }

    /// <summary>
    /// カメラの正面に向けてSphereCastを飛ばし、敵の検知を行う。
    /// outパラメータでヒット情報を返すため、呼び出し側で詳細な座標や対象を取得可能。
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

    /// <summary>
    /// デバッグ用：Scene Viewで選択時に検知範囲のギズモを描画する
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (_fpsCam == null) return;

        Gizmos.color = Color.cyan;
        Vector3 origin = _fpsCam.transform.position;
        Vector3 end = origin + _fpsCam.transform.forward * _detectDistance;

        // 始点と終点の球、およびその間を結ぶ線を描画してSphereCastの軌跡を可視化
        Gizmos.DrawWireSphere(origin, _detectRange);
        Gizmos.DrawLine(origin, end);
        Gizmos.DrawWireSphere(end, _detectRange);
    }
}