using UnityEngine;

/// <summary>
/// 息を止める（ホールド）能力。止めている間は足音・呼吸ノイズを抑えて敵に気づかれにくくする。
/// 息ゲージを使い切ると強制的に「あえぎ（息切れ）」状態になり、一時的に大きな音を立てる。
/// 実際のノイズ反映は PlayerStealthStatus が本コンポーネントの状態を参照して行う。
/// </summary>
[RequireComponent(typeof(InputPlayerController))]
public class PlayerBreath : MonoBehaviour
{
    [Header("息止め設定")]
    // 息を止められる最大秒数
    [SerializeField] private float _breathMax = 4f;
    // 息ゲージの回復量（秒/秒）
    [SerializeField] private float _breathRegenRate = 1.5f;
    // ゲージ切れで起きる「あえぎ」の持続秒数
    [SerializeField] private float _gaspDuration = 1.2f;
    // あえぎ中の足音半径（大きいほど見つかりやすい）
    [SerializeField] private float _gaspNoiseRadius = 8f;

    private InputPlayerController _input;
    private bool _holdKeyHeld;
    private float _breath;
    private float _gaspTimer;

    /// 現在、息を止めているか。
    public bool IsHoldingBreath { get; private set; }

    ///ゲージ切れによるあえぎ（息切れ）中か。
    public bool IsGasping => _gaspTimer > 0f;

    /// あえぎ中に適用する足音半径。
    public float GaspNoiseRadius => _gaspNoiseRadius;

    private void Awake()
    {
        _input = GetComponent<InputPlayerController>();
        _breath = _breathMax;
    }

    private void OnEnable()
    {
        _input.OnHoldBreathPerformed += HandleHoldBreath;
    }

    private void OnDisable()
    {
        _input.OnHoldBreathPerformed -= HandleHoldBreath;
        _holdKeyHeld = false;
    }

    private void HandleHoldBreath(bool pressed) => _holdKeyHeld = pressed;

    private void Update()
    {
        float dt = Time.deltaTime;

        if (_gaspTimer > 0f)
        {
            // あえぎ中は息止め不可・回復もしない
            _gaspTimer -= dt;
            IsHoldingBreath = false;
            return;
        }

        if (_holdKeyHeld && _breath > 0f)
        {
            IsHoldingBreath = true;
            _breath -= dt;
            if (_breath <= 0f)
            {
                _breath = 0f;
                IsHoldingBreath = false;
                _gaspTimer = _gaspDuration;  // 限界に達したらあえぎへ
            }
        }
        else
        {
            IsHoldingBreath = false;
            if (_breath < _breathMax)
                _breath = Mathf.Min(_breath + _breathRegenRate * dt, _breathMax);
        }
    }
}
