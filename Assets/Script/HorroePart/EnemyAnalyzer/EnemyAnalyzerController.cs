using UnityEngine;

/// <summary>
/// カメラ構えの入力を受け取り、解析の開始・停止とUIの表示切替を管理する。
/// InputPlayerController と EnemyAnalyzer の橋渡し役。
/// </summary>
public class EnemyAnalyzerController : MonoBehaviour
{
    private static readonly int BarrelPowerId = Shader.PropertyToID("_BarrelPower");

    [SerializeField] private InputPlayerController _inputController;
    [SerializeField] private EnemyAnalyzer _analyzer;
    [SerializeField] private CanvasGroup _analyzerUIGroup;
    [SerializeField] private EnemyDetector _detector;
    [SerializeField] private HandLightController _handLightController;
    // カメラUIのフェード速度
    [SerializeField] private float _uiFadeSpeed = 8f;

    [Header("魚眼レンズ")]
    [SerializeField] private Material _fisheyeMaterial;                 // RendererFeatureと同じ魚眼マテリアル
    [SerializeField] private float _aimBarrelPower = 0.2f;              // 構えたときの魚眼レンズの強さ

    private bool _isAiming = false;

    private void Awake()
    {
        DebugCustom.ValidateFields(this,
            (nameof(_inputController), _inputController),
            (nameof(_analyzer), _analyzer),
            (nameof(_analyzerUIGroup), _analyzerUIGroup),
            (nameof(_detector), _detector),
            (nameof(_handLightController), _handLightController),
            (nameof(_fisheyeMaterial), _fisheyeMaterial));

        _analyzerUIGroup.alpha          = 0f;
        _analyzerUIGroup.interactable   = false;
        _analyzerUIGroup.blocksRaycasts = false;

        // ゲーム開始時は魚眼レンズを無効化（マテリアルの値は前回実行から残るため確実にリセット）
        if (_fisheyeMaterial != null)
            _fisheyeMaterial.SetFloat(BarrelPowerId, 0f);
    }

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

        // 構えているときだけ魚眼レンズを有効化（一瞬で切り替え）
        if (_fisheyeMaterial != null)
            _fisheyeMaterial.SetFloat(BarrelPowerId, isAiming ? _aimBarrelPower : 0f);

        // 構えを解除したらリセット
        if (isAiming)
            _handLightController.ForceOff();
        else
            _analyzer.Reset();
    }
}