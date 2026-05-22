using UnityEngine;
using HorrorGame.Interaction;
using HorrorGame.UI;

namespace HorrorGame.Interaction
{
    /// <summary>
    /// インタラクトするとメモ本文を MemoUIPresenter に渡して画面に表示するアイテム。
    /// 何度でも読み返せる仕様。1度しか読ませたくない場合は
    /// OnInteract() 内で gameObject.SetActive(false) を追加すること。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MemoItem : MonoBehaviour, IInteractable
    {
        [SerializeField][TextArea(3, 10)] private string memoContent;
        [SerializeField] private MemoUIPresenter uiPresenter;

        public bool CanInteract => true;
        public string HintText => "メモを読む";

        public void OnInteract()
        {
            if (uiPresenter == null)
            {
                DebugCustom.LogWarning("[MemoItem] MemoUIPresenter が未設定です。");
                return;
            }

            uiPresenter.Show(memoContent);
        }
    }
}