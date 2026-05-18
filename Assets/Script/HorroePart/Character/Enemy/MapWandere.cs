using System.Collections.Generic;
using DungeonSystem;
using UnityEngine;

/// <summary>
/// 通路セル（GridType.Corridor）のワールド座標をウェイポイントとして順に移動する徘徊クラス。
/// EnemySpawnerからSetGridで通路座標リストを受け取り、ランダムに次の目標を選び続ける。
/// 部屋内に留まらず通路を優先的に歩くことを目的としている。
/// </summary>
public class MapWanderer : MonoBehaviour, IEnemyBehavior
{
    [SerializeField] private float wanderSpeed = 2f;
    [SerializeField] private float rotateSpeed = 10f;

    // ウェイポイントに到達したと判定する距離（グリッドサイズより小さい値に設定すること）
    [SerializeField] private float arrivalRadius = 1.0f;

    private Rigidbody _rb;

    // 全Corridorセルのワールド座標リスト（EnemySpawnerから注入）
    private List<Vector3> _corridorWaypoints = new List<Vector3>();

    private Vector3 _currentTarget;
    private bool _isInitialized;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// グリッド情報を受け取り、Corridorセルのワールド座標リストを構築する。
    /// EnemySpawnerがInstantiate直後に呼び出すこと。
    /// </summary>
    public void SetGrid(GridType[,] grid, float gridSize)
    {
        _corridorWaypoints.Clear();

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                if (grid[x, y] != GridType.Corridor) continue;

                // セル中心のワールド座標を登録する
                _corridorWaypoints.Add(new Vector3(
                    (x + 0.5f) * gridSize,
                    0f,
                    (y + 0.5f) * gridSize
                ));
            }
        }

        if (_corridorWaypoints.Count == 0)
        {
            DebugCustom.LogWarning("[MapWanderer] 通路セルが見つかりませんでした。徘徊を停止します。");
            return;
        }

        _currentTarget = PickRandomWaypoint();
        _isInitialized = true;
    }

    public void Tick()
    {
        if (!_isInitialized) return;

        // 現在のウェイポイントに到達したら次をランダムに選ぶ
        if (IsArrived())
            _currentTarget = PickRandomWaypoint();

        MoveTowardTarget();
    }

    private bool IsArrived()
    {
        var diff = _currentTarget - transform.position;
        // Y軸は無視してXZ平面上の距離で判定する
        return diff.x * diff.x + diff.z * diff.z <= arrivalRadius * arrivalRadius;
    }

    private Vector3 PickRandomWaypoint()
    {
        return _corridorWaypoints[Random.Range(0, _corridorWaypoints.Count)];
    }

    private void MoveTowardTarget()
    {
        var diff = _currentTarget - transform.position;
        var direction = new Vector3(diff.x, 0f, diff.z).normalized;

        if (direction == Vector3.zero) return;

        var targetRot = Quaternion.LookRotation(direction);
        _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime);

        var vel = _rb.linearVelocity;
        _rb.linearVelocity = new Vector3(direction.x * wanderSpeed, vel.y, direction.z * wanderSpeed);
    }
}