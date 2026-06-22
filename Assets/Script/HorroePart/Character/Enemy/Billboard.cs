using UnityEngine;

// 平面オブジェクト（Sprite等）を常にメインカメラへ正面を向けるコンポーネント。
// 地面に立つキャラクターや木を想定し、ピッチ（上下の傾き）は変化させず
// ヨー（左右）のみ追従させることで、カメラを見上げ/見下げても不自然に傾かないようにしている。
public class Billboard : MonoBehaviour
{
    private Transform _cameraTransform;

    private void Start()
    {
        // 実行中にメインカメラが切り替わらない前提でキャッシュ
        if (Camera.main == null)
        {
            DebugCustom.LogError("[Billboard] MainCamera が見つかりません。", this);
            enabled = false;
            return;
        }
        _cameraTransform = Camera.main.transform;
    }

    // カメラの移動・回転が確定した後に向きを合わせたいため、Updateではなく LateUpdate を使う。
    private void LateUpdate()
    {
        if (_cameraTransform == null) return;
        AlignToCamera();
    }

    private void AlignToCamera()
    {
        Vector3 directionToCamera = _cameraTransform.position - transform.position;
        directionToCamera.y = 0f;

        if (directionToCamera.sqrMagnitude < Mathf.Epsilon)
        {
            return;
        }

        transform.forward = directionToCamera.normalized;
    }
}