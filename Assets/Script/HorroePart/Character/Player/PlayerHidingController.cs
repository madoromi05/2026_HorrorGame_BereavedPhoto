using System.Collections;
using HorrorGame.Player;
using UnityEngine;

/// <summary>
/// ベッド下・クローゼット内など隠れ場所への出入りを制御する。
///
/// 隠れ中の視点操作:
///   PlayerCamera / PlayerMover は無効化してこのクラスが代わりに OnLookPerformed を処理する。
///   - HidingSpot が設定した角度範囲（_yawRange / _pitchRange）でヨー・ピッチを制限する。
///   - 外→内レイキャストで HidingSpot の子コライダーを検出し、壁への視点通り抜けをブロックする。
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(InputPlayerController))]
public class PlayerHidingController : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float     _transitionDuration   = 0.35f;
    [SerializeField] private float     _hideLookSensitivity  = 0.1f;
    // 外→内レイキャストの長さ。隠れ場所の壁までの最大距離より少し小さく設定する
    [SerializeField] private float     _colliderCheckDist    = 0.25f;

    public bool        IsHiding   { get; private set; }
    public HidingSpot  ActiveSpot { get; private set; }

    private CharacterController  _cc;
    private PlayerMover          _mover;
    private PlayerCamera         _playerCamera;
    private PlayerCrouch         _crouch;
    private PlayerDashController _dash;
    private PlayerInteractor     _interactor;
    private InputPlayerController _input;
    private Renderer[]           _playerRenderers;

    // 隠れ中のカメラ姿勢管理
    private Quaternion _hideBaseRot;   // アンカーの回転（ニュートラル基準）
    private float      _hideYaw;
    private float      _hidePitch;
    private float      _viewYawLeft, _viewYawRight, _viewPitchMin, _viewPitchMax;
    private bool       _isTransitioning;

    // 退出後の復帰用
    private Vector3    _savedCameraLocalPos;
    private Quaternion _savedCameraLocalRot;
    private Coroutine  _currentCoroutine;

    private void Awake()
    {
        _cc           = GetComponent<CharacterController>();
        _mover        = GetComponent<PlayerMover>();
        _playerCamera = GetComponent<PlayerCamera>();
        _crouch       = GetComponent<PlayerCrouch>();
        _dash         = GetComponent<PlayerDashController>();
        _interactor   = GetComponent<PlayerInteractor>();
        _input        = GetComponent<InputPlayerController>();

        _playerRenderers = GetComponentsInChildren<Renderer>(true);
        DebugCustom.ValidateFields(this, (nameof(_cameraTransform), _cameraTransform));
    }

    private void OnEnable()
    {
        _input.OnInteractPerformed += HandleInteractInput;
        _input.OnLookPerformed     += HandleHideLook;
    }

    private void OnDisable()
    {
        _input.OnInteractPerformed -= HandleInteractInput;
        _input.OnLookPerformed     -= HandleHideLook;
    }

    // ── 外部 API ─────────────────────────────────────────────

    public void EnterHide(Transform cameraAnchor, HidingSpot spot)
    {
        if (IsHiding) return;

        IsHiding   = true;
        ActiveSpot = spot;

        _savedCameraLocalPos = _cameraTransform.localPosition;
        _savedCameraLocalRot = _cameraTransform.localRotation;

        // 視点のニュートラル基準＋可動域をスポットから取得
        _hideBaseRot = cameraAnchor.rotation;
        _hideYaw     = 0f;
        _hidePitch   = 0f;
        var lim = spot.ViewLimits;
        (_viewYawLeft, _viewYawRight, _viewPitchMin, _viewPitchMax) = lim;

        SetMovementEnabled(false);
        SetRenderersEnabled(false);
        _interactor?.SetForcedHint(spot.HintText); // "出る [E]" を表示

        if (_currentCoroutine != null) StopCoroutine(_currentCoroutine);
        _currentCoroutine = StartCoroutine(
            TransitionCameraWorld(cameraAnchor.position, cameraAnchor.rotation));
    }

    public void ExitHide()
    {
        if (!IsHiding) return;

        IsHiding   = false;
        ActiveSpot = null;
        _interactor?.SetForcedHint(null); // 強制ヒントを解除して通常検出に戻す

        if (_currentCoroutine != null) StopCoroutine(_currentCoroutine);
        _currentCoroutine = StartCoroutine(RestoreCameraAndEnable());
    }

    // ── 入力ハンドラ ──────────────────────────────────────────

    private void HandleInteractInput()
    {
        if (IsHiding) ExitHide();
    }

    /// <summary>
    /// 隠れ中のみ視点を操作する。
    /// 角度範囲クランプ → 子コライダー衝突チェックの順で適用する。
    /// </summary>
    private void HandleHideLook(Vector2 input)
    {
        if (!IsHiding || _isTransitioning) return;

        // 角度範囲でクランプ
        float newYaw   = Mathf.Clamp(
            _hideYaw   + input.x * _hideLookSensitivity,
            -_viewYawLeft, _viewYawRight);

        // 候補回転を計算
        float newPitch = Mathf.Clamp(
                 _hidePitch + input.y * _hideLookSensitivity,
                 _viewPitchMin, _viewPitchMax);
        Quaternion candidateRot = _hideBaseRot * Quaternion.Euler(-newPitch, newYaw, 0f);
        Vector3    lookDir      = candidateRot * Vector3.forward;

        // 子コライダーで遮蔽されていなければ適用
        if (!IsBlockedByChildCollider(lookDir))
        {
            _hideYaw   = newYaw;
            _hidePitch = newPitch;
            _cameraTransform.rotation = candidateRot;
        }
    }

    /// <summary>
    /// 外→内レイキャストで HidingSpot の子コライダーを検出する。
    /// カメラ内部からのキャストは裏面に当たらない場合があるため、
    /// lookDir 方向に _colliderCheckDist 進んだ点から逆方向に撃つ。
    /// </summary>
    private bool IsBlockedByChildCollider(Vector3 lookDir)
    {
        if (ActiveSpot == null) return false;

        Vector3 outerOrigin = _cameraTransform.position + lookDir * _colliderCheckDist;
        if (Physics.Raycast(outerOrigin, -lookDir, out RaycastHit hit,
                _colliderCheckDist, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            return hit.collider.transform.IsChildOf(ActiveSpot.transform);
        }
        return false;
    }

    // ── 内部ユーティリティ ────────────────────────────────────

    private void SetMovementEnabled(bool enabled)
    {
        if (_mover        != null) _mover.enabled        = enabled;
        if (_playerCamera != null) _playerCamera.enabled = enabled;  // 視点は自前で処理
        if (_crouch       != null) _crouch.enabled       = enabled;
        if (_dash         != null) _dash.enabled         = enabled;
        if (_interactor   != null) _interactor.enabled   = enabled;
        _cc.enabled = enabled;
    }

    // カメラをワールド座標で目標位置へ補間する（隠れるとき）
    private IEnumerator TransitionCameraWorld(Vector3 targetPos, Quaternion targetRot)
    {
        _isTransitioning = true;
        Vector3    startPos = _cameraTransform.position;
        Quaternion startRot = _cameraTransform.rotation;
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Clamp01(t + Time.deltaTime / _transitionDuration);
            float s = Mathf.SmoothStep(0f, 1f, t);
            _cameraTransform.position = Vector3.Lerp(startPos, targetPos, s);
            _cameraTransform.rotation = Quaternion.Slerp(startRot, targetRot, s);
            yield return null;
        }
        _isTransitioning = false;
    }

    // カメラをローカル座標で元の位置へ戻し、完了後に移動を再有効化する（出るとき）
    private IEnumerator RestoreCameraAndEnable()
    {
        _isTransitioning = true;
        Vector3    startLocalPos = _cameraTransform.localPosition;
        Quaternion startLocalRot = _cameraTransform.localRotation;
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Clamp01(t + Time.deltaTime / _transitionDuration);
            float s = Mathf.SmoothStep(0f, 1f, t);
            _cameraTransform.localPosition = Vector3.Lerp(startLocalPos, _savedCameraLocalPos, s);
            _cameraTransform.localRotation = Quaternion.Slerp(startLocalRot, _savedCameraLocalRot, s);
            yield return null;
        }
        _isTransitioning = false;
        SetRenderersEnabled(true);
        SetMovementEnabled(true);
    }

    private void SetRenderersEnabled(bool enabled)
    {
        foreach (var r in _playerRenderers)
            r.enabled = enabled;
    }
}
