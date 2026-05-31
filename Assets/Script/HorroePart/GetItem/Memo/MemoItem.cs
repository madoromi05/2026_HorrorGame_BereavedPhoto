using HorrorGame.Interaction;
using HorrorGame.Item;
using HorrorGame.UI;
using UnityEngine;

namespace HorrorGame.Interaction
{
    /// <summary>
    /// インタラクトするとメモ本文をMemoUIPresenterに渡して画面に表示するアイテム。
    /// 何度でも読み返せる仕様。1度だけ読んだら消す場合は
    /// OnInteract() 内に gameObject.SetActive(false) を追加すること。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MemoItem : MonoBehaviour, IInteractable
    {
        [SerializeField][TextArea(3, 10)] private string _memoContent;
        [SerializeField] private ItemData _itemData;
        public bool CanInteract => true;
        public string HintText => "メモを読む";

        private MemoUIPresenter _uiPresenter;
        private Inventory _inventory;

        public void Init(MemoUIPresenter presenter, Inventory inventory)
        {
            _uiPresenter = presenter;
            _inventory = inventory;
        }

        private void Start()
        {
            // ItemInitializer 経由で Init() されない場合（シーン直置き等）のフォールバック
            if (_uiPresenter == null)
                _uiPresenter = FindFirstObjectByType<MemoUIPresenter>();
            if (_inventory == null)
                _inventory = FindFirstObjectByType<Inventory>();
        }

        public void OnInteract()
        {
            if (_uiPresenter == null)
            {
                DebugCustom.LogWarning("[MemoItem] MemoUIPresenter がシーン内に見つかりません。");
                return;
            }

            _uiPresenter.Show(_memoContent);
            _inventory.AddItem(_itemData);
        }
    }
}
