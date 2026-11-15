using UnityEngine;

/// <summary>
/// カメラ構えの入力を受け取り、解析の開始・停止とUIの表示切替を管理する
/// InputPlayerController と EnemyAnalyzer の橋渡し役
/// </summary>
public class EnemyAnalyzerController : MonoBehaviour
{
    [SerializeField] private InputPlayerController _inputController;
    [SerializeField] private EnemyAnalyzer _analyzer;
    [SerializeField] private CanvasGroup _analyzerUIGroup;
    [SerializeField] private EnemyDetector _detector;
    [SerializeField] private HandLightController _handLightController;
    // カメラUIのフェード速度
    [SerializeField] private float _uiFadeSpeed = 8f;

    [Header("魚眼レンズ")]
    [SerializeField] private float _aimBarrelPower = 0.2f;              // 構えたときの魚眼レンズの強さ

    private bool _isAiming = false;

    private void Awake()
    {
        DebugCustom.ValidateFields(this,
            (nameof(_inputController), _inputController),
            (nameof(_analyzer), _analyzer),
            (nameof(_analyzerUIGroup), _analyzerUIGroup),
            (nameof(_detector), _detector),
            (nameof(_handLightController), _handLightController));

        _analyzerUIGroup.alpha          = 0f;
        _analyzerUIGroup.interactable   = false;
        _analyzerUIGroup.blocksRaycasts = false;

        FisheyeDistortionFeature.SetBarrelPower(0f);	// 実行時の複製はシーンを跨いで残るため、始めに戻す
    }

    private void OnEnable()
    {
        _inputController.OnCameraPerformed += HandleCamera;
    }

    private void OnDisable()
    {
        _inputController.OnCameraPerformed -= HandleCamera;
        FisheyeDistortionFeature.SetBarrelPower(0f);	// 構えたままシーンを抜けても次のシーンへ残さない
    }

    private void Update()
    {
        // UIをフェードイン/アウト
        float targetAlpha = _isAiming ? 1f : 0f;
        _analyzerUIGroup.alpha = Mathf.MoveTowards(
            _analyzerUIGroup.alpha,
            targetAlpha,
            _uiFadeSpeed * Time.deltaTime
        );

        // 構えていないときは解析を止める
        if (!_isAiming)
            _analyzer.SetEnemyInRange(false);
    }

    // 敵解析開始時の処理
    private void HandleCamera(bool isAiming)
    {
        _isAiming = isAiming;

        _detector.SetAiming(isAiming);
        _analyzer.SetAiming(isAiming);

        FisheyeDistortionFeature.SetBarrelPower(isAiming ? _aimBarrelPower : 0f);	// 構えているときだけ歪ませる

        // 構えを解除したらリセット
        if (isAiming)
            _handLightController.ForceOff();
        else
            _analyzer.ResetAimState();
    }
}