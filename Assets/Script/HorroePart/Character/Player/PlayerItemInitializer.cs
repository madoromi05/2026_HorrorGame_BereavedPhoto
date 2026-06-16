using HorrorGame.Item;
using UnityEngine;

/// <summary>
/// ゲーム開始時にプレイヤーの初期アイテムを付与するコンポーネント。
/// Player プレハブにアタッチして使用する。
/// </summary>
public class PlayerItemInitializer : MonoBehaviour
{
    [SerializeField] private int _initialObstructionItemCount = 5;

    private void Start()
    {
        GetComponent<Inventory>()
            ?.InitializeItems(ItemType.ObstructionItem, _initialObstructionItemCount);
    }
}
