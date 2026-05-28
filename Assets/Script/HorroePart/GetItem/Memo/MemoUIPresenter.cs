using TMPro;
using UnityEngine;

namespace HorrorGame.UI
{
    /// <summary>
    /// メモの本文を画面中央にオーバーレイ表示するUIクラス。
    /// Canvasの下のパネルにアタッチし、MemoItem から Show() を呼ぶ。
    /// Eキーまたは長押しボタンで非表示になる。
    /// </summary>
    public class MemoUIPresenter : RevealUIPresenterBase
    {
        [SerializeField] private TextMeshProUGUI _memoText;

        /// <summary>
        /// メモ本文を受け取りオーバーレイを表示する。MemoItemから呼ばれる。
        /// </summary>
        public void Show(string content)
        {
            _memoText.text = content;
            ShowBase();
        }
    }
}
