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
    private int _raycastMask;
    private void Awake()
    {
        DebugCustom.ValidateFields(this,
            (nameof(_analyzer), _analyzer),

            (nameof(_fpsCam), _fpsCam));
        // Physics.DefaultRaycastLayers から "Player" レイヤーを除外したマスクを計算
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1)
        {
            _raycastMask = Physics.DefaultRaycastLayers & ~(1 << playerLayer);
        }
        else
        {
            DebugCustom.LogWarning("[EnemyDetector] 'Player' レイヤーが存在しません。デフォルトのマスクを使用します。");
            _raycastMask = Physics.DefaultRaycastLayers;
        }
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
            _analyzer.SetMapEnemyInView(false);
            return;
        }

        // エイム中の場合は敵の検知を試みる
        if (TryDetectEnemy(out RaycastHit hit))
        {
            // 子コライダーがヒットしても常にルートの EnemyController を使う
            var root = hit.collider.GetComponentInParent<EnemyController>();

            // MapWanderer（通路徘徊敵）は解析不可。UIメッセージだけ表示する
            if (root != null && root.GetComponent<MapWanderer>() != null)
            {
                _analyzer.SetCurrentEnemy(null);
                _analyzer.SetEnemyInRange(false);
                _analyzer.SetMapEnemyInView(true);
            }
            else
            {
                _analyzer.SetCurrentEnemy(root != null ? root.gameObject : hit.collider.gameObject);
                _analyzer.SetEnemyInRange(true);
                _analyzer.SetMapEnemyInView(false);
            }
        }
        else
        {
            _analyzer.SetCurrentEnemy(null);
            _analyzer.SetEnemyInRange(false);
            _analyzer.SetMapEnemyInView(false);
        }
    }

    /// <summary>
    /// カメラの正面に向けてSphereCastを飛ばし、敵の検知を行う。
    /// outパラメータでヒット情報を返すため、呼び出し側で詳細な座標や対象を取得可能。
    /// </summary>
    private bool TryDetectEnemy(out RaycastHit hit)
    {
        Ray ray = new Ray(_fpsCam.transform.position, _fpsCam.transform.forward);

        // まずSphereCastで敵を検知する
        if (Physics.SphereCast(ray, _detectRange, out hit, _detectDistance, _enemyLayer))
        {
            // カメラから敵のヒット位置へ細いRayを飛ばす
            Vector3 origin = _fpsCam.transform.position;
            Vector3 direction = hit.point - origin;
            float distance = direction.magnitude;

            // 計算済みの _raycastMask を使用し、Playerレイヤーを無視する
            if (Physics.Raycast(origin, direction.normalized, out RaycastHit sightHit, distance, _raycastMask, QueryTriggerInteraction.Ignore))
            {
                var targetEnemy = hit.collider.GetComponentInParent<EnemyController>();
                var blockingEnemy = sightHit.collider.GetComponentInParent<EnemyController>();

                // Raycastが当たった物体が「敵ではない（壁など）」または「別の敵」だった場合は、遮蔽されているとみなす
                if (blockingEnemy == null || blockingEnemy != targetEnemy)
                {
                    return false;
                }
            }

            return true;
        }

        return false;
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