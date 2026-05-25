using UnityEngine;

/// <summary>
/// カメラ構えの入力を受け取り、解析の開始・停止とUIの表示切替を管理する。
/// InputPlayerController と EnemyAnalyzer の橋渡し役。
/// </summary>
public class EnemyAnalyzerController : MonoBehaviour
{
    [SerializeField] private InputPlayerController inputController;
    [SerializeField] private EnemyAnalyzer analyzer;
    [SerializeField] private CanvasGroup analyzerUIGroup;
    [SerializeField] private EnemyDetector detector;
    [SerializeField] private HandLightController handLightController;
    // カメラUIのフェード速度
    [SerializeField] private float uiFadeSpeed = 8f;

    private bool m_isAiming = false;

    private void OnEnable()
    {
        inputController.OnCameraPerformed += HandleCamera;
    }

    private void OnDisable()
    {
        inputController.OnCameraPerformed -= HandleCamera;
    }

    private void Update()
    {
        // UIをフェードイン/アウト
        float targetAlpha = m_isAiming ? 1f : 0f;
        analyzerUIGroup.alpha = Mathf.MoveTowards(
            analyzerUIGroup.alpha,
            targetAlpha,
            uiFadeSpeed * Time.deltaTime
        );

        // 構えていないときは解析を止める
        if (!m_isAiming)
            analyzer.SetEnemyInRange(false, null);
    }

    private void HandleCamera(bool isAiming)
    {
        m_isAiming = isAiming;

        detector.SetAiming(isAiming);
        analyzer.SetAiming(isAiming);

        // 構えを解除したらリセット
        if (isAiming)
            handLightController.ForceOff();
        else
            analyzer.Reset();
    }
}