using UnityEngine;
using UnityEngine.SceneManagement;
using HorrorGame.Interaction;
using HorrorGame.Item;

namespace HorrorGame.Interaction
{
    /// <summary>
    /// Doorオブジェクトにアタッチするコンポーネント。
    /// 対応する ItemType の鍵を Inventory に所持している場合のみ
    /// ScenarioScene へ遷移するドア。
    /// requiredKey は Inspector で対応する KeyItem の ItemType と一致させること。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ScenarioDoor : MonoBehaviour, IInteractable
    {
        private const string kScenarioSceneName = "ScenarioPart";

        [SerializeField] private ItemType requiredKey;

        public bool CanInteract => true;
        public string HintText => "扉に鍵を使う";
        public void OnInteract()
        {
            var inventory = FindFirstObjectByType<Inventory>();
            if (inventory == null)
            {
                DebugCustom.LogWarning("[ScenarioDoor] Inventory が見つかりません。");
                return;
            }

            if (!inventory.HasItem(requiredKey))
            {
                DebugCustom.Log($"[ScenarioDoor] 鍵 '{requiredKey}' を持っていないため開けられない。");
                return;
            }

            SceneManager.LoadScene(kScenarioSceneName);
        }
    }
}