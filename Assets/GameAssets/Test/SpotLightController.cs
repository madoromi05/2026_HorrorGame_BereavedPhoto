using UnityEngine;

public class SpotLightController : MonoBehaviour
{
    // パラメータを更新したいマテリアル
    [SerializeField] private Material material;

    // スポットライトを追従させたいTransform
    // [SerializeField] private Transform lookTarget;

    // 更新するシェーダープロパティのID
    private readonly int shaderID_TargetPosition = Shader.PropertyToID("_TargetPosition");

    private void Update()
    {
        if (material == null) return;
        //if (lookTarget == null) return;

        //material.SetVector(shaderID_TargetPosition, lookTarget.position);
    }
}