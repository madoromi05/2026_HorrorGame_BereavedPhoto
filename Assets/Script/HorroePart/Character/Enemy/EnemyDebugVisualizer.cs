using UnityEngine;

/// <summary>
/// DebugMode 時に EnemyLook が有効なとき、エネミーのメッシュを半透明の発光マテリアルで上書きする。
/// ZTest を Always に設定することで壁越しにも視認できるようにする。
/// RoomWanderer / MapWanderer の有無でアタッチ先に色を振り分ける。
/// </summary>
public class EnemyDebugVisualizer : MonoBehaviour
{
    private static readonly Color kRoomWandererColor = new Color(1.0f, 0.5f, 0.1f, 0.85f); // オレンジ
    private static readonly Color kMapWandererColor = new Color(0.2f, 1.0f, 1.0f, 0.85f); // シアン
    private static readonly Color kUnknownColor = new Color(1.0f, 1.0f, 1.0f, 0.85f); // 白

    /// <summary>
    /// EnemySpawner が Instantiate 後に呼び出す。
    /// アタッチされている Behavior コンポーネントに応じて発光色を選びマテリアルを差し替える。
    /// </summary>
    public void Apply()
    {
        var color = ResolveColor();
        ApplyToRenderers(color);
    }

    private Color ResolveColor()
    {
        if (GetComponent<RoomWanderer>() != null) return kRoomWandererColor;
        if (GetComponent<MapWanderer>() != null) return kMapWandererColor;
        return kUnknownColor;
    }

    private void ApplyToRenderers(Color color)
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            var shader = FindTransparentShader();
            var mat = new Material(shader);
            ApplyColor(mat, color);
            renderer.material = mat;
        }
    }

    private static Shader FindTransparentShader()
    {
        return Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Standard")
            ?? Shader.Find("Diffuse");
    }

    private static void ApplyColor(Material mat, Color color)
    {
        if (mat.HasProperty("_BaseColor"))
        {
            // URP 系
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);

            // ZTest を Always にして壁の裏からでも描画されるようにする
            mat.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);

            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2.0f);
        }
        else
        {
            // Built-in Standard
            mat.SetColor("_Color", color);
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);

            // ZTest を Always にして壁越しに視認できるようにする
            mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);

            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2.0f);
        }

        // 通常の不透明物より後に描画し ZTest=Always の効果を確実にする
        mat.renderQueue = 4000;
    }
}