using HorrorGame.Item;
using TMPro;
using UnityEngine;

/// <summary>
/// 妨害アイテムの所持数とアイコンを表示する HUD コンポーネント。
/// 所持数が 1 個以上のときのみ表示する。Canvas にアタッチして使用する。
/// </summary>
public class ObstructionItemHUD : MonoBehaviour
{
    [SerializeField] private GameObject _root;      // アイコン＋テキストを含む HUD 全体のルート
    [SerializeField] private TMP_Text   _countText;

    private Inventory _inventory;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        _inventory = player?.GetComponent<Inventory>();

        if (_inventory == null)
        {
            DebugCustom.LogWarning("[ObstructionItemHUD] Inventory が見つかりません。");
            _root?.SetActive(false);
            return;
        }

        _inventory.OnItemCountChanged += OnCountChanged;
        Refresh();
    }

    private void OnDestroy()
    {
        if (_inventory != null)
            _inventory.OnItemCountChanged -= OnCountChanged;
    }

    private void OnCountChanged(ItemType type, int _)
    {
        if (type == ItemType.ObstructionItem) Refresh();
    }

    private void Refresh()
    {
        int count = _inventory.GetItemCount(ItemType.ObstructionItem);
        _root?.SetActive(count > 0);
        if (_countText != null)
            _countText.text = count.ToString();
    }
}
