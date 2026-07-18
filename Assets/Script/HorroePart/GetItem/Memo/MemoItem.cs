using HorrorGame.Interaction;
using HorrorGame.Item;
using HorrorGame.UI;
using UnityEngine;

namespace HorrorGame.Interaction
{
    /// <summary>
    /// インタラクトするとメモ本文をMemoUIPresenterに渡して画面に表示するアイテム。
    /// 複数ページに対応し、Eキー長押しでページ送り・最終ページで閉じる。
    /// 何度でも読み返せる仕様。1度だけ読んだら消す場合は
    /// OnInteract() 内に gameObject.SetActive(false) を追加すること。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MemoItem : MonoBehaviour, IInteractable
    {
        // 1要素 = 1ページ。Inspectorで要素を追加するとページが増える。
        [SerializeField][TextArea(3, 10)] private string[] _memoPages;
        [SerializeField] private ItemData _itemData;
        public bool CanInteract => true;
        public string HintText => "【E】本を読む";

        private MemoUIPresenter _uiPresenter;
        private Inventory _inventory;

        public void Init(MemoUIPresenter presenter, Inventory inventory)
        {
            _uiPresenter = presenter;
            _inventory = inventory;
        }

        public void OnInteract()
        {
            if (_uiPresenter == null)
            {
                DebugCustom.LogWarning("[MemoItem] MemoUIPresenter がシーン内に見つかりません。");
                return;
            }

            // 最初に本を見た時、チュートリアル用の操作UIが有効なら非表示にしてから表示処理を開始する。
            OperationTutorialUI.HideIfActive();

            _uiPresenter.Show(_memoPages);

            // 他のアイテムと取得SEを統一する。何度も読み返せるため初回取得時のみ鳴らす。
            bool alreadyOwned = _itemData != null && _inventory.HasItem(_itemData.ItemType);
            _inventory.AddItem(_itemData);
            if (!alreadyOwned)
                AudioManager.Instance?.PlaySe(SeType.ItemPickup);
        }
    }
}
