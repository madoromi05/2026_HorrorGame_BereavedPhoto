using UnityEngine;
using System.Collections;

/// <summary>
/// キャラクターのしゃがみ状態（コライダーとカメラの高さ）を制御する。
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(InputPlayerController))]
public class PlayerCrouch : MonoBehaviour
{
    [Header("しゃがみ設定")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _crouchHeightOffset = 2.0f;  // しゃがみの高さ
    [SerializeField] private float _crouchTransitionSpeed = 2f; // しゃがみのトランジション速度

    // インスペクターで視覚的に編集できるカーブ
    [SerializeField] private AnimationCurve _crouchCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public bool IsCrouching { get; private set; }

    private CharacterController _characterController;
    private InputPlayerController _inputCallbackController;

    private float _standHeight;         // 立ち状態のコライダーの高さ
    private float _cameraStandLocalY;   // 立ち状態のカメラのローカルY座標

    private Coroutine _crouchCoroutine;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _inputCallbackController = GetComponent<InputPlayerController>();
        _standHeight = _characterController.height;

        if (_cameraTransform != null) {
            _cameraStandLocalY = _cameraTransform.localPosition.y;
        }
    }

    private void OnEnable()
    {
        _inputCallbackController.OnCrouchPerformed += HandleCrouch;
    }

    private void OnDisable()
    {
        _inputCallbackController.OnCrouchPerformed -= HandleCrouch;
    }

    private void HandleCrouch(bool isCrouching)
    {
        IsCrouching = isCrouching;
        float targetHeight = IsCrouching ? _standHeight - _crouchHeightOffset : _standHeight;
        float targetCameraY = IsCrouching ? _cameraStandLocalY - _crouchHeightOffset : _cameraStandLocalY;

        if (_crouchCoroutine != null)
        {
            StopCoroutine(_crouchCoroutine);
        }

        _crouchCoroutine = StartCoroutine(CrouchRoutine(targetHeight, targetCameraY));
    }

    /// <summary>
    /// しゃがみ状態を強制的に解除する。ダッシュ開始時などに外部から呼ぶ。
    /// しゃがみ中のときだけ HandleCrouch(false) を実行する。
    /// </summary>
    public void CancelCrouch()
    {
        if (IsCrouching)
            HandleCrouch(false);
    }

    /// <summary>
    /// 目標の高さ（targetHeight）とカメラ位置（targetCameraY）に向けて値を変化させる。
    /// 経過時間を使用してS字カーブ（SmoothStep）を適用し、人間らしい自然な加減速を再現する。
    /// </summary>
    private IEnumerator CrouchRoutine(float targetHeight, float targetCameraY)
    {
        // 遷移開始時の現在値を記録しておく
        float startHeight = _characterController.height;
        float startCameraY = _cameraTransform != null ? _cameraTransform.localPosition.y : 0f;

        // 進行度（0.0 〜 1.0）
        float t = 0f;

        // t が 1（完了）に達するまでループ
        while (t < 1f)
        {
            // 毎フレーム進行度を加算（_crouchTransitionSpeed が大きいほど速く 1 に到達する）
            t += Time.deltaTime * _crouchTransitionSpeed;

            // t を 0〜1 の範囲内に収める
            float clampedT = Mathf.Clamp01(t);

            // S字カーブ（動き出しと止まる直前を滑らかにする）を適用
            float curveT = _crouchCurve.Evaluate(clampedT);

            // 記録した開始値と目標値の間を、カーブを適用した進行度で補間
            float newHeight = Mathf.Lerp(startHeight, targetHeight, curveT);

            float newCameraY = 0f;
            if (_cameraTransform != null)
            {
                newCameraY = Mathf.Lerp(startCameraY, targetCameraY, curveT);
            }

            // 計算した値を実際に適用
            ApplyCrouchState(newHeight, newCameraY);

            yield return null;
        }
    }

    /// <summary>
    /// CharacterControllerの高さとカメラのY座標を実際に適用する
    /// </summary>
    private void ApplyCrouchState(float height, float cameraY)
    {
        _characterController.height = height;
        _characterController.center = new Vector3(0, height / 2f, 0);

        if (_cameraTransform != null)
        {
            Vector3 localPos = _cameraTransform.localPosition;
            _cameraTransform.localPosition = new Vector3(localPos.x, cameraY, localPos.z);
        }
    }
}
