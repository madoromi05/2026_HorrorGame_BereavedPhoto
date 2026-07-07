using TMPro;
using UnityEngine;

namespace HorrorGame.UI
{
    /// <summary>
    /// メモの本文を画面中央にオーバーレイ表示するUIクラス。
    /// Canvasの下のパネルにアタッチし、MemoItem から Show() を呼ぶ。
    /// 複数ページに対応し、Eキー長押しで次のページへ、
    /// 最終ページで長押しすると閉じる。
    /// </summary>
    public class MemoUIPresenter : RevealUIPresenterBase
    {
        [SerializeField] private TextMeshProUGUI _memoText;
        [SerializeField] private TextMeshProUGUI _pageCountText;

        private string[] _pages;
        private int _currentPage;

        private void Awake()
        {
            if (_memoText == null)
                DebugCustom.LogError($"[MemoUIPresenter] _memoText が未設定です。", this);
        }

        /// <summary>
        /// メモ本文（複数ページ）を受け取りオーバーレイを表示する。MemoItemから呼ばれる。
        /// </summary>
        public void Show(string[] pages)
        {
            if (pages == null || pages.Length == 0)
            {
                DebugCustom.LogWarning("[MemoUIPresenter] 表示するページがありません。");
                return;
            }

            _pages = pages;
            _currentPage = 0;
            ApplyPage();
            ShowBase();
        }

        /// <summary>
        /// 長押し完了時。次のページがあれば送り、無ければ閉じる。
        /// </summary>
        protected override void OnHoldComplete()
        {
            if (_currentPage < _pages.Length - 1)
            {
                _currentPage++;
                ApplyPage();
                return;
            }

            base.OnHoldComplete(); // 最終ページなので閉じる
        }

        private void ApplyPage()
        {
            _memoText.text = _pages[_currentPage];
            if (_pageCountText != null)
                _pageCountText.text = $"{_currentPage + 1} / {_pages.Length}";
        }
    }
}
