using HorrorGame.Interaction;
using UnityEngine;

/// <summary>
/// ベッドやクローゼットなど、プレイヤーが身を潜める隠れ場所。
/// IInteractable を実装し PlayerInteractor から呼ばれる。
///
/// セットアップ:
///   1. ベッド/クローゼットの GameObject にこのコンポーネントを付ける。
///   2. 子 Transform (_cameraAnchor) を隠れ中に見える視点位置・向きに配置する。
///      ベッド下なら床スレスレ・部屋の外を覗く方向（水平〜やや下向き）、クローゼットなら扉の隙間から外を覗く向きが自然。
///      ※ 天井向き（Rotation X = -90°付近）にすると視点が上になってしまうため注意。
///   3. インタラクト可能なコライダーを _interactLayer に設定する。
/// </summary>
public class HidingSpot : MonoBehaviour, IInteractable
{
    public enum HidingType { Bed, Closet }

    [SerializeField] private HidingType _hidingType;
    [SerializeField] private Transform  _cameraAnchor;

    [Header("隠れ中のカメラ可動域（子コライダーの形状に合わせて設定）")]
    [SerializeField] private float _yawRangeLeft  = 40f;  // アンカー正面から左への最大角度
    [SerializeField] private float _yawRangeRight = 40f;  // アンカー正面から右への最大角度
    [SerializeField] private float _pitchMin      = -5f;   // 下方向への最大角度（負値）
    [SerializeField] private float _pitchMax      = 30f;  // 上方向への最大角度

    /// PlayerHidingController が隠れ中の視点制限に使う可動域
    public (float yawLeft, float yawRight, float pitchMin, float pitchMax) ViewLimits
        => (_yawRangeLeft, _yawRangeRight, _pitchMin, _pitchMax);

    // ExitDoor と同様に、初回アクセス時にシーン内から自動取得する
    private PlayerHidingController _hidingController;
    private PlayerHidingController HidingController =>
        _hidingController ??= FindFirstObjectByType<PlayerHidingController>();

    private static readonly string[] EnterHints =
    {
        "ベッドに隠れる",
        "クローゼットに隠れる",
    };

    private const string ExitHint = "出る [E]";

    public bool CanInteract =>
        HidingController != null &&
        (!HidingController.IsHiding || HidingController.ActiveSpot == this);

    public string HintText =>
        HidingController != null && HidingController.ActiveSpot == this
            ? ExitHint
            : EnterHints[(int)_hidingType];

    public void OnInteract()
    {
        if (HidingController == null || _cameraAnchor == null)
        {
            if (_cameraAnchor == null)
                DebugCustom.LogWarning("[HidingSpot] _cameraAnchor が未設定です。", this);
            return;
        }

        if (HidingController.ActiveSpot == this)
            HidingController.ExitHide();
        else
            HidingController.EnterHide(_cameraAnchor, this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_cameraAnchor == null) return;

        Vector3 origin  = _cameraAnchor.position;
        float   rayLen  = 0.4f;

        // アンカー中心点
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(origin, 0.04f);

        // ヨー可動域（左右）
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.8f);
        Quaternion leftRot  = _cameraAnchor.rotation * Quaternion.Euler(0f, -_yawRangeLeft,  0f);
        Quaternion rightRot = _cameraAnchor.rotation * Quaternion.Euler(0f,  _yawRangeRight, 0f);
        Gizmos.DrawRay(origin, leftRot  * Vector3.forward * rayLen);
        Gizmos.DrawRay(origin, rightRot * Vector3.forward * rayLen);

        // ピッチ可動域（上下）
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        Quaternion upRot   = _cameraAnchor.rotation * Quaternion.Euler(_pitchMin, 0f, 0f);
        Quaternion downRot = _cameraAnchor.rotation * Quaternion.Euler(_pitchMax, 0f, 0f);
        Gizmos.DrawRay(origin, upRot   * Vector3.forward * rayLen);
        Gizmos.DrawRay(origin, downRot * Vector3.forward * rayLen);

        // 正面方向
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin, _cameraAnchor.forward * rayLen);
    }
#endif
}
