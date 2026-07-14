using UnityEngine;

/// <summary>
/// シーンに1つ配置し、割り当てた VhsProfile を VHS ポストエフェクトへ適用する。
/// profile.enabled=false のプロファイルを割り当てれば、そのシーンでは VHS を無効化できる。
/// </summary>
public class VhsController : MonoBehaviour
{
    [SerializeField] private VhsProfile profile;

    // シーン読み込み時（有効化時）にプロファイルを VHS の入口へ渡す。
    private void OnEnable() => VhsFeature.Apply(profile);
}
