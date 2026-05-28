using UnityEngine;

/// <summary>
/// カメラ構えの入力を受け取り、解析の開始・停止とUIの表示切替を管理する。
/// InputPlayerController と EnemyAnalyzer の橋渡し役。
/// </summary>
public class EnemyAnalyzerController : MonoBehaviour
{
    [SerializeField] private InputPlayerController _inputController;
    [SerializeField] private EnemyAnalyzer _analyzer;
    [SerializeField] private CanvasGroup _analyzerUIGroup;
    [SerializeField] private EnemyDetector _detector;
    [SerializeField] private HandLightController _handLightController;
    // カメラUIのフェード速度
    [SerializeField] private float uiFadeSpeed = 8f;

    private bool m_isAiming = false;

    private void OnEnable()
    {
        _inputController.OnCameraPerformed += HandleCamera;
    }

    private void OnDisable()
    {
        _inputController.OnCameraPerformed -= HandleCamera;
    }

    private void Update()
    {
        // UIをフェードイン/アウト
        float targetAlpha = m_isAiming ? 1f : 0f;
        _analyzerUIGroup.alpha = Mathf.MoveTowards(
            _analyzerUIGroup.alpha,
            targetAlpha,
            uiFadeSpeed * Time.deltaTime
        );

        // 構えていないときは解析を止める
        if (!m_isAiming)
            _analyzer.SetEnemyInRange(false);
    }

    private void HandleCamera(bool isAiming)
    {
        m_isAiming = isAiming;

        _detector.SetAiming(isAiming);
        _analyzer.SetAiming(isAiming);

        // 構えを解除したらリセット
        if (isAiming)
            _handLightController.ForceOff();
        else
            _analyzer.Reset();

    }
}