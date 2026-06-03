using UnityEngine;

/// <summary>
/// スポットライトの光線を粒子エフェクトで可視化する。
/// 広い空間でもライトが点いているのがわかるようにする。
/// HandLightController と同じ GameObject にアタッチすること。
/// </summary>
[RequireComponent(typeof(HandLightController))]
public class LightBeamEffect : MonoBehaviour
{
    [Header("ビームエフェクト設定")]
    [SerializeField] private float _particleEmitRate = 20f;              // 1秒あたりの放出数
    [SerializeField] private float _particleLifetime = 3.5f;             // パーティクルの生存時間（秒）
    [SerializeField] private float _particleSize = 0.04f;                // パーティクルの大きさ
    [SerializeField] private Color _particleColor = Color.white;          // パーティクルの色
    [SerializeField, Range(0f, 1f)] private float _particleAlpha = 0.2f; // 透明度（0=透明, 1=不透明）
    [SerializeField] private float _startOffset = 0.5f;                  // ライト原点からパーティクル発生開始までの距離（m）

    private HandLightController _lightController;
    private Light _spotLight;
    private ParticleSystem _beamParticles;

    private void Awake()
    {
        _lightController = GetComponent<HandLightController>();

    }

    private void Start()
    {
        // handLight の子から SpotLight を探す
        _spotLight = GetComponentInChildren<Light>(includeInactive: true);
        if (_spotLight == null) return;

        _beamParticles = CreateBeamParticles(_spotLight.transform);
    }

    private void Update()
    {
        if (_beamParticles == null) return;

        bool shouldEmit = _lightController.IsLightOn;

        var emission = _beamParticles.emission;
        emission.enabled = shouldEmit;

        if (shouldEmit)
            SyncWithSpotLight();
    }

    private void SyncWithSpotLight()
    {
        // スポットライトの角度・距離に合わせてコーン形状を更新
        var shape = _beamParticles.shape;
        shape.angle = _spotLight.spotAngle * 0.5f;

        // オフセット分だけ短い距離を速度に反映
        float travelDistance = Mathf.Max(0.1f, _spotLight.range - _startOffset);
        var main = _beamParticles.main;
        main.startSpeedMultiplier = travelDistance / _particleLifetime;
    }

    private ParticleSystem CreateBeamParticles(Transform parent)
    {
        var go = new GameObject("LightBeamParticles");
        go.transform.SetParent(parent, worldPositionStays: false);
        // ライト前方にオフセットして出現位置をずらす
        go.transform.localPosition = new Vector3(0f, 0f, _startOffset);
        go.transform.localRotation = Quaternion.identity;

        var ps = go.AddComponent<ParticleSystem>();
        var renderer = go.GetComponent<ParticleSystemRenderer>();

        float travelDistance = Mathf.Max(0.1f, _spotLight.range - _startOffset);

        // --- Main ---
        var main = ps.main;
        main.loop = true;
        main.startLifetime = _particleLifetime;
        main.startSpeed = travelDistance / _particleLifetime;
        main.startSize = _particleSize;
        main.startColor = new Color(_particleColor.r, _particleColor.g, _particleColor.b);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

        // --- Emission ---
        var emission = ps.emission;
        emission.rateOverTime = _particleEmitRate;

        // --- Shape: コーン形状でスポットライトに合わせる ---
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = _spotLight.spotAngle * 0.5f;
        shape.radius = 0.05f;
        shape.radiusThickness = 1f;

        // --- Color over Lifetime: 先端に向かってフェードアウト ---
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(_particleAlpha, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = gradient;

        // --- Renderer: 加算合成で光っぽく見せる ---
        renderer.material = CreateAdditiveMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 1;
        renderer.minParticleSize = 0.005f;
        renderer.maxParticleSize = 1.5f;

        ps.Play();
        return ps;
    }

    private Material CreateAdditiveMaterial()
    {
        Shader shader = Shader.Find("Particles/Additive")
                     ?? Shader.Find("Particles/Standard Unlit")
                     ?? Shader.Find("Sprites/Default");

        var mat = new Material(shader);

        // 透明・加算合成モードを強制設定（シェーダーのデフォルトが不透明のため）
        mat.SetFloat("_Mode", 2f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3000;

        mat.color = Color.white;
        mat.enableInstancing = true;
        return mat;
    }
}
