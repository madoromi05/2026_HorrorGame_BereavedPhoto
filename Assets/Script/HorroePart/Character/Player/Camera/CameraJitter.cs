using UnityEngine;

/// <summary>
/// PS1 風の微細なカメラ位置ジッターを加える。
/// 毎フレームランダムにカメラ位置をわずかにずらすことで
/// 「オブジェクトが不安定に揺れて見える」チラズアート的な質感を作る。
/// </summary>
public class CameraJitter : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;

    [Header("ジッター強度")]
    [SerializeField, Range(0f, 0.015f)] private float _jitterAmount = 0.004f;

    // カメラの基準ローカル座標（ボブなど他スクリプトが動かしていない前提）
    private Vector3 _baseLocalPosition;

    private void Awake()
    {
        if (_cameraTransform == null)
            _cameraTransform = GetComponentInChildren<Camera>()?.transform;

        if (_cameraTransform != null)
            _baseLocalPosition = _cameraTransform.localPosition;
        else
            DebugCustom.LogError("[PS1CameraJitter] _cameraTransform が見つかりません。", this);
    }

    private void LateUpdate()
    {
        if (_cameraTransform == null) return;

        float x = (Random.value - 0.5f) * 2f * _jitterAmount;
        float y = (Random.value - 0.5f) * 2f * _jitterAmount;
        _cameraTransform.localPosition = _baseLocalPosition + new Vector3(x, y, 0f);
    }

    /// <summary>外部から基準位置を更新する（ボブ等と併用する場合）。</summary>
    public void SetBaseLocalPosition(Vector3 pos) => _baseLocalPosition = pos;
}
