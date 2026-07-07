using UnityEngine;

/// <summary>
/// StartRoom の Prefab 内に置くプレイヤー初期スポーン位置マーカー。
/// この Transform の位置と向き（Y 軸 Yaw）がそのまま生成時のプレイヤー配置に使われる。
/// Prefab Mode でドラッグ・回転して自由に配置できる。複数あった場合は最初の 1 つを使用する。
/// SectionPlacer がルーム生成後にこのコンポーネントを検索して参照する。
/// </summary>
public class PlayerSpawnPoint : MonoBehaviour
{
    // マーカーのワールド座標。
    public Vector3 Position => transform.position;

    // マーカーの向きから求めた Yaw（Y 軸回転・度）。プレイヤーの初期向きに使う。
    public float Yaw => transform.eulerAngles.y;
}
