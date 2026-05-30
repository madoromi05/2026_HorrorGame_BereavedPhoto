using UnityEngine;

/// <summary>
/// 壁の角から身を乗り出して覗き見る（リーン）。
/// カメラ(_cameraTransform)の localPosition.X だけを左右にずらして横覗きを表現する。
///
/// PlayerCrouch はカメラの localPosition.Y を、PlayerCamera はカメラの回転(pitch)を操作するため、
/// 本クラスが X だけを更新する限りそれらと競合しない（Y・Z・回転には触れない）。
/// ロールを付けたい場合は、カメラとは別の専用 Transform を _rollTarget に割り当てる
///（カメラ本体に割り当てると PlayerCamera の pitch 更新でロールが打ち消されるため不可）。
///
/// 注意: _cameraTransform に Player ルート（CharacterController/移動の本体）を割り当ててはいけない。
/// 毎フレーム localPosition を書き換えるため、移動・スポーン位置が打ち消される。誤設定時は自動で無効化する。
/// </summary>
[RequireComponent(typeof(InputPlayerController))]
public class PlayerLean : MonoBehaviour
{
    [Header("リーン設定")]
    // 横へずらすカメラ。Player 直下の Main Camera を割り当てる（ルートは不可）
    [SerializeField] private Transform _cameraTransform;
    // 横へずらす距離(m)
    [SerializeField] private float _leanDistance = 0.4f;
    // ロール角(度)。_rollTarget を割り当てたときのみ適用
    [SerializeField] private float _leanAngle = 10f;
    // 補間速度（大きいほど素早く乗り出す）
    [SerializeField] private float _leanSpeed = 8f;
    // ロール（傾き）用の任意 Transform。未設定ならロールなし。カメラ本体は不可
    [SerializeField] private Transform _rollTarget;

    private InputPlayerController _input;
    private bool _leftHeld;
    private bool _rightHeld;
    private float _currentLean;   // -1(左)〜+1(右) の補間後の現在値
    private float _baseCameraX;

    private void Awake()
    {
        _input = GetComponent<InputPlayerController>();

        // 安全装置: 自分（移動の本体）の Transform を割り当てていたら無効化して移動破壊を防ぐ
        if (_cameraTransform == transform)
        {
            DebugCustom.LogWarning($"[PlayerLean] _cameraTransform に Player ルートが割り当てられています。" +
                                   $"Main Camera を割り当ててください。リーンを無効化します: {gameObject.name}");
            _cameraTransform = null;
        }

        if (_cameraTransform != null)
            _baseCameraX = _cameraTransform.localPosition.x;
    }

    private void OnEnable()
    {
        _input.OnLeanLeftPerformed += HandleLeanLeft;
        _input.OnLeanRightPerformed += HandleLeanRight;
    }

    private void OnDisable()
    {
        _input.OnLeanLeftPerformed -= HandleLeanLeft;
        _input.OnLeanRightPerformed -= HandleLeanRight;
    }

    private void HandleLeanLeft(bool pressed)  => _leftHeld = pressed;
    private void HandleLeanRight(bool pressed) => _rightHeld = pressed;

    private void Update()
    {
        if (_cameraTransform == null) return;

        // 左右同時押しは相殺。左 = -1、右 = +1
        float target = (_rightHeld ? 1f : 0f) - (_leftHeld ? 1f : 0f);
        _currentLean = Mathf.Lerp(_currentLean, target, _leanSpeed * Time.deltaTime);

        // X だけ更新（Y は PlayerCrouch が、回転は PlayerCamera が操作するので触れない）
        var pos = _cameraTransform.localPosition;
        pos.x = _baseCameraX + _currentLean * _leanDistance;
        _cameraTransform.localPosition = pos;

        // ロールは専用 Transform がある場合のみ
        if (_rollTarget != null)
            _rollTarget.localEulerAngles = new Vector3(0f, 0f, -_currentLean * _leanAngle);
    }
}
