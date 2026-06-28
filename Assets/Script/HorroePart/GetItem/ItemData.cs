using UnityEngine;

namespace HorrorGame.Item
{
    /// <summary>
    /// アイテム1種類の静的データを保持するScriptableObject。
    /// Inspectorで作成し、ItemPickupにアタッチして使用する。
    /// 実行時に変更されることは想定していない（読み取り専用データ）。
    /// </summary>
    [CreateAssetMenu(fileName = "ItemData", menuName = "ItemData")]
    public class ItemData : ScriptableObject
    {
        [SerializeField] private ItemType _itemType;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;

        [TextArea(2, 5)]
        [SerializeField] private string _description;

        public ItemType ItemType => _itemType;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public string Description => _description;
    }
}
