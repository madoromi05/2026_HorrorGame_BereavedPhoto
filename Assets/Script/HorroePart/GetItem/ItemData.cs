using UnityEngine;

namespace HorrorGame.Item
{
    /// <summary>
    /// アイテム1種類の静的データを保持するScriptableObject。
    /// Inspectorで作成し、ItemPickupにアタッチして使用する。
    /// ランタイムで変更されることは想定していない（読み取り専用データ）。
    /// </summary>
    [CreateAssetMenu(fileName = "ItemData", menuName = "ItemData")]
    public class ItemData : ScriptableObject
    {
        [SerializeField] private ItemType itemType;
        [SerializeField] private string displayName;

        [TextArea(2, 5)]
        [SerializeField] private string description;

        public ItemType ItemType => itemType;
        public string DisplayName => displayName;
        public string Description => description;
    }
}