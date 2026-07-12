using UnityEngine;

/// <summary>
/// 敵にプレイヤーが発見された瞬間と追跡中の画面演出を管理する。
///
/// 発見時: ヴィネットを強化し、カメラを一度強く揺らす。
/// 追跡中に移動しているとき: カメラを軽く揺らし続ける。
/// </summary>
public class DetectionCameraEffects : MonoBehaviour
{
    [SerializeField] private PlayerCamera _playerCamera;

    [Header("発見時の強い揺れ（一回）")]
    [SerializeField] private float _burstDuration   = 0.6f;
    [SerializeField] private float _burstMagnitude  = 5f;   // 最大揺れ角度（度）
    [SerializeField] private float _burstFrequency  = 22f;  // ノイズ周波数（速さ）

    [Header("追跡中の移動揺れ")]
    [SerializeField] private float _walkMagnitude  = 1.2f;  // 揺れ角度（度）
    [SerializeField] private float _walkFrequency  = 7f;

    [Header("通常歩行ヘッドボブ")]
    [SerializeField] private float _bobMagnitude = 0.35f;  // 揺れ角度（度）
    [SerializeField] private float _bobFrequency = 4f;     // 周波数（速さ）

    [Header("ライトボブ")]
    [SerializeField] private HandLightController _handLightController;
    [SerializeField] private GameObject _handLight;
    [SerializeField] private float _lightBobMagnitude = 0.04f;  // ローカル座標（m）

    private PlayerMover _playerMover;
    private EnemyController[] _enemies;
    private float _enemyRefreshTimer;

    private bool  _wasChased;
    private float _burstTimer;
    private float _perlinSeedX;
    private float _perlinSeedY;
    private Vector3 _handLightInitialLocalPos;

    private void Awake()
    {
        _playerMover = GetComponent<PlayerMover>();

        if (_playerCamera == null)
            _playerCamera = GetComponentInChildren<PlayerCamera>();
        if (_playerCamera == null)
            DebugCustom.LogError("[DetectionCameraEffects] PlayerCamera が見つかりません。", this);

        if (_handLightController == null)
            _handLightController = GetComponent<HandLightController>();

        if (_handLight != null)
            _handLightInitialLocalPos = _handLight.transform.localPosition;

        _perlinSeedX = Random.value * 100f;
        _perlinSeedY = Random.value * 100f;
    }

    private void Update()
    {
        RefreshEnemiesIfNeeded();

        bool isChased = IsAnyChasing();

        // 追跡開始を検出したらバーストシェイクをトリガー
        if (isChased && !_wasChased)
            _burstTimer = _burstDuration;

        _wasChased = isChased;

        bool isMoving = _playerMover != null &&
                        _playerMover.CurrentMoveState != PlayerMover.MoveState.Idle;

        // シェイク計算 → PlayerCamera に注入
        if (_playerCamera != null)
            _playerCamera.SetShakeAngle(ComputeShake(isChased, isMoving));

        // ライトボブ
        ApplyLightBob(isMoving, isChased);
    }

    private Vector3 ComputeShake(bool isChased, bool isMoving)
    {
        // 発見時の強い一回揺れ（タイマーが残っている間、先頭が最強で減衰）
        if (_burstTimer > 0f)
        {
            _burstTimer -= Time.deltaTime;
            float decay = Mathf.Max(0f, _burstTimer) / _burstDuration;
            float t = Time.time * _burstFrequency;
            float x = (Mathf.PerlinNoise(_perlinSeedX + t, 0f)      - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0f, _perlinSeedY + t * 0.9f) - 0.5f) * 2f;
            return new Vector3(x, y, 0f) * (_burstMagnitude * decay);
        }

        // 追跡中 + 移動中の軽い揺れ（優先）
        if (isChased && isMoving)
        {
            float t = Time.time * _walkFrequency;
            float x = Mathf.Sin(t * 1.1f) * _walkMagnitude;
            float y = Mathf.Abs(Mathf.Sin(t * 0.7f)) * (_walkMagnitude * 0.4f);
            return new Vector3(x, y, 0f);
        }

        // 通常歩行ヘッドボブ
        if (isMoving)
        {
            var state = _playerMover.CurrentMoveState;
            float freq = state == PlayerMover.MoveState.Dash ? _bobFrequency * 1.4f : _bobFrequency;
            float mag  = state == PlayerMover.MoveState.Dash ? _bobMagnitude * 1.4f : _bobMagnitude;
            float t = Time.time * freq;
            float x = Mathf.Sin(t * 2f) * mag;
            float y = Mathf.Sin(t) * (mag * 0.35f);
            return new Vector3(x, y, 0f);
        }

        return Vector3.zero;
    }

    private void ApplyLightBob(bool isMoving, bool isChased)
    {
        bool hasController = _handLightController != null;
        bool hasFlashlight = _handLight != null;
        if (!hasController && !hasFlashlight) return;

        if (!isMoving || isChased)
        {
            if (hasController) _handLightController.SetBobOffset(Vector3.zero);
            if (hasFlashlight) _handLight.transform.localPosition = _handLightInitialLocalPos;
            return;
        }

        var state = _playerMover.CurrentMoveState;
        float freq = state == PlayerMover.MoveState.Dash ? _bobFrequency * 1.4f : _bobFrequency;
        float mag = state == PlayerMover.MoveState.Dash ? _lightBobMagnitude * 1.4f : _lightBobMagnitude;

        float t = Time.time * freq;
        var offset = new Vector3(Mathf.Sin(t) * (mag * 0.35f), Mathf.Sin(t * 2f) * mag, 0f);

        if (hasController) _handLightController.SetBobOffset(offset);
        if (hasFlashlight) _handLight.transform.localPosition = _handLightInitialLocalPos + offset;
    }

    private void RefreshEnemiesIfNeeded()
    {
        _enemyRefreshTimer -= Time.deltaTime;
        if (_enemyRefreshTimer > 0f) return;
        _enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        _enemyRefreshTimer = 3f;
    }

    private bool IsAnyChasing()
    {
        if (_enemies == null) return false;
        foreach (var e in _enemies)
            if (e != null && e.IsActivated)
                return true;
        return false;
    }
}
