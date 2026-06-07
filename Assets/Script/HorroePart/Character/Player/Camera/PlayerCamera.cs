using UnityEngine;

/// <summary>
/// FPSカメラの上下回転のみを制御する。
/// Y軸回転はFPSMoverが制御する。
/// </summary>
[RequireComponent(typeof(InputPlayerController))]
public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _pitchSensitivity = 0.1f;
    [SerializeField] private float _minPitch = -90f;
    [SerializeField] private float _maxPitch = 90f;

    [Header("PS1 カメラジッター")]
    [SerializeField, Range(0f, 0.5f)] private float _jitterDegrees = 0.15f;

    private float _currentPitch;
    private Vector3 _shakeAngle;
    private InputPlayerController _inputCallbackController;

    private void Awake()
    {
        _inputCallbackController = GetComponent<InputPlayerController>();

        if (_cameraTransform == null)
            DebugCustom.LogError($"[PlayerCamera] _cameraTransform が未設定です。", this);

        // 保存済み感度を読み込む（未保存なら Inspector 値をそのまま使う）
        if (PlayerPrefs.HasKey(OptionMenuController.KeySens))
            _pitchSensitivity = PlayerPrefs.GetFloat(OptionMenuController.KeySens);
    }

    /// <summary>オプションメニューからマウス感度を即時反映する。</summary>
    public void SetSensitivity(float v) => _pitchSensitivity = v;

    /// <summary>DetectionCameraEffects からシェイク角度（度）を毎フレーム注入する。</summary>
    public void SetShakeAngle(Vector3 angle) => _shakeAngle = angle;

    private void OnEnable()
    {
        _inputCallbackController.OnLookPerformed += HandleLook;
    }

    private void OnDisable()
    {
        _inputCallbackController.OnLookPerformed -= HandleLook;
    }

    private void HandleLook(Vector2 input)
    {
        // 上下回転（Pitch）のみ更新。Y軸回転はFPSMoverが制御
        _currentPitch -= input.y * _pitchSensitivity;
        _currentPitch = Mathf.Clamp(_currentPitch, _minPitch, _maxPitch);
    }

    private void LateUpdate()
    {
        if (_cameraTransform == null) return;
        // シェイク角度を合成してカメラに反映（Y軸シェイクはローカルなので左右の微揺れになる）
        float jx = (Random.value - 0.5f) * 2f * _jitterDegrees;
        float jy = (Random.value - 0.5f) * 2f * _jitterDegrees;
        _cameraTransform.localEulerAngles = new Vector3(_currentPitch + _shakeAngle.x + jx, _shakeAngle.y + jy, 0f);
    }
}
