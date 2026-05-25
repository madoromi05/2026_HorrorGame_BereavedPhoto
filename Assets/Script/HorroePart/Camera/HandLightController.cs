using System.Collections;
using UnityEngine;

/// <summary>
/// 懐中電灯の点灯・消灯・警告点滅・自動消灯を管理する。
/// 連続点灯時間が上限に達すると警告点滅を経て自動消灯し、
/// 手動でOFFにするまで再点灯できない。
/// </summary>
public class HandLightController : MonoBehaviour
{
    [SerializeField] private GameObject handLight;

    [Header("点灯時間設定")]
    [SerializeField] private float maxLightDuration = 30f;   // 連続点灯できる上限時間（秒）
    [SerializeField] private float flickerStartTime = 5f;    // 消灯の何秒前から点滅を開始するか
    [SerializeField] private float flickerInterval = 0.2f;   // 点滅間隔（秒）

    // カメラを構えた時の強制解除
    public void ForceOff() => TurnOff();

    private InputPlayerController _inputController;
    private bool isLightOn = false;
    private float lightOnTimer = 0f;

    // 警告点滅コルーチンの参照（手動OFFで中断するために保持）
    private Coroutine flickerCoroutine;

    private void Awake()
    {
        _inputController = GetComponent<InputPlayerController>();
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
        if (!isLightOn) return;

        lightOnTimer += Time.deltaTime;

        // 点滅開始タイミングに達したらコルーチンを起動（二重起動を防ぐ）
        bool shouldFlicker = lightOnTimer >= maxLightDuration - flickerStartTime;
        if (shouldFlicker && flickerCoroutine == null)
        {
            flickerCoroutine = StartCoroutine(FlickerThenTurnOff());
        }
    }

    /// <summary>
    /// トグル入力を受けてON/OFFを切り替える。
    /// 点滅中でも手動OFFを受け付け、タイマーをリセットする。
    /// </summary>
    private void HandleHandLightToggle()
    {
        if (isLightOn)
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
        isLightOn = true;
        lightOnTimer = 0f;
        handLight.SetActive(true);
    }

    /// <summary>
    /// 手動・自動どちらのOFFでも呼ばれる共通処理。
    /// 点滅コルーチンを確実に停止してタイマーをリセットする。
    /// </summary>
    private void TurnOff()
    {
        isLightOn = false;
        lightOnTimer = 0f;
        handLight.SetActive(false);

        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }
    }

    /// <summary>
    /// 警告点滅を行い、完走したら自動消灯する。
    /// 手動OFFが入った場合はTurnOff()側でコルーチンが停止される。
    /// </summary>
    private IEnumerator FlickerThenTurnOff()
    {
        float remainingTime = maxLightDuration - lightOnTimer;

        while (remainingTime > 0f)
        {
            handLight.SetActive(!handLight.activeSelf);
            yield return new WaitForSeconds(flickerInterval);
            remainingTime -= flickerInterval;
        }

        TurnOff();
    }
}