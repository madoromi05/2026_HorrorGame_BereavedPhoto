using TMPro;
using UnityEngine;

namespace HorrorGame.UI
{
    /// <summary>
    /// メモの本文を画面中央にオーバーレイ表示するUI制御クラス。
    /// Canvas配下のパネルにアタッチし、MemoItem から Show() を呼ぶ。
    /// Eキーまたは閉じるボタンで非表示になる。
    /// </summary>
    public class MemoUIPresenter : RevealUIPresenterBase
    {
        [SerializeField] private TextMeshProUGUI memoText;

        /// <summary>
        /// メモ本文を受け取りオーバーレイを表示する。MemoItemから呼ばれる。
        /// </summary>
        public void Show(string content)
        {
            memoText.text = content;
            ShowBase();
        }
    }
}