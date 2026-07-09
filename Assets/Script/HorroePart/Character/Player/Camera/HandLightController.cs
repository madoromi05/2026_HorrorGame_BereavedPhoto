using System.Collections;
using UnityEngine;

/// <summary>
/// 手持ちライトの点灯・消灯・点滅を一元管理する。
/// 点灯時間が上限に達すると点滅してから自動消灯し、
/// 手動でOFFにするまで再点灯できない。
/// </summary>
public class HandLightController : MonoBehaviour
{
    [Header("ライトオブジェクト設定")]
    [Tooltip("手からとらすライトのスポットライトを持つオブジェクト")]
    [SerializeField] private GameObject handLight;

    [Header("点灯時間設定")]
    [SerializeField] private float _maxLightDuration = 30f;   // 最大点灯できる時間（秒）
    [SerializeField] private float _flickerStartTime = 5f;    // 残り何秒前から点滅を開始するか
    [SerializeField] private float _flickerInterval = 0.2f;   // 点滅間隔（秒）

    // カメラから呼び出せる公開メソッド
    public void ForceOff() => TurnOff();
    // 外部からの点灯状態を参照
    public bool IsLightOn => _isLightOn;

    private InputPlayerController _inputController;
    private Light _mainLightComponent;

    private bool _isLightOn = false;
    private float _lightOnTimer = 0f;
    private Vector3 _initialLocalPos;

    // 点滅コルーチンの参照（手動OFFで中断するために保持）
    private Coroutine _flickerCoroutine;

    private void Awake()
    {
        _inputController = GetComponent<InputPlayerController>();

        if (handLight != null)
        {
            _mainLightComponent = handLight.GetComponent<Light>();
            _initialLocalPos = handLight.transform.localPosition;
        }
        handLight?.SetActive(false);
    }

    private void OnEnable()
    {
        _inputController.OnHandLightPerformed += HandleHandLightToggle;
    }

    private void OnDisable()
    {
        _inputController.OnHandLightPerformed -= HandleHandLightToggle;
    }

    private void Update()
    {
        if (!_isLightOn) return;

        _lightOnTimer += Time.deltaTime;

        // 点滅開始タイミングに達したらコルーチンを起動（二重起動を防ぐ）
        bool shouldFlicker = _lightOnTimer >= _maxLightDuration - _flickerStartTime;
        if (shouldFlicker && _flickerCoroutine == null)
        {
            _flickerCoroutine = StartCoroutine(FlickerThenTurnOff());
        }
    }

    /// <summary>
    /// トグル入力を受けてON/OFFを切り替える。
    /// 点滅中でも手動OFFを受け付け、タイマーをリセットする。
    /// </summary>
    private void HandleHandLightToggle()
    {
        if (_isLightOn)
        {
            TurnOff();
        }
        else
        {
            TurnOn();
        }
    }

    private void TurnOn()
    {
        _isLightOn = true;
        _lightOnTimer = 0f;
        SetLightActive(true);
        AudioManager.Instance?.PlaySe(SeType.HandLightToggle);
    }

    /// <summary>
    /// 手動・自動どちらから呼ばれても共通処理。
    /// 点滅コルーチンを確実に停止してタイマーをリセットする。
    /// </summary>
    private void TurnOff()
    {
        // 既に消灯済みなら何もしない（構え時の ForceOff などで余計な切り替えSEを鳴らさない）
        if (!_isLightOn) return;

        _isLightOn = false;
        _lightOnTimer = 0f;
        SetLightActive(false);
        AudioManager.Instance?.PlaySe(SeType.HandLightToggle);

        if (_flickerCoroutine != null)
        {
            StopCoroutine(_flickerCoroutine);
            _flickerCoroutine = null;
        }
    }

    /// <summary>
    /// 点滅を実行し、時間切れで自動消灯する。
    /// </summary>
    private IEnumerator FlickerThenTurnOff()
    {
        float remainingTime = _maxLightDuration - _lightOnTimer;

        // 消えかかりの開始を知らせるSE（点滅開始時に一度だけ）
        AudioManager.Instance?.PlaySe(SeType.HandLightFlicker);

        while (remainingTime > 0f)
        {
            bool nextState = !(handLight != null && handLight.activeSelf);
            SetLightActive(nextState);

            yield return new WaitForSeconds(_flickerInterval);
            remainingTime -= _flickerInterval;
        }

        TurnOff();
    }

    /// <summary>歩行ボブのオフセットをローカル座標で加算する。</summary>
    public void SetBobOffset(Vector3 localOffset)
    {
        if (handLight != null)
            handLight.transform.localPosition = _initialLocalPos + localOffset;
    }

    private void SetLightActive(bool isActive)
    {
        if (handLight != null) handLight.SetActive(isActive);
    }
}
