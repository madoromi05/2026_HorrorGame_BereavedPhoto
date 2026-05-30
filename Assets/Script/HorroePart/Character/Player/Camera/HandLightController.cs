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

    // 点滅コルーチンの参照（手動OFFで中断するために保持）
    private Coroutine _flickerCoroutine;

    private void Awake()
    {
        _inputController = GetComponent<InputPlayerController>();

        if (handLight != null)
        {
            _mainLightComponent = handLight.GetComponent<Light>();
        }
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
    }

    /// <summary>
    /// 手動・自動どちらから呼ばれても共通処理。
    /// 点滅コルーチンを確実に停止してタイマーをリセットする。
    /// </summary>
    private void TurnOff()
    {
        _isLightOn = false;
        _lightOnTimer = 0f;
        SetLightActive(false);

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

        while (remainingTime > 0f)
        {
            bool nextState = !(handLight != null && handLight.activeSelf);
            SetLightActive(nextState);

            yield return new WaitForSeconds(_flickerInterval);
            remainingTime -= _flickerInterval;
        }

        TurnOff();
    }

    private void SetLightActive(bool isActive)
    {
        if (handLight != null) handLight.SetActive(isActive);
    }
}
