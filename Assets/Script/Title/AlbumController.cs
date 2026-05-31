using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// アルバム UI を管理するコントローラー。
/// ページの解放状態に応じてロック / アンロック表示を切り替える。
/// </summary>
public class AlbumController : MonoBehaviour
{
    [System.Serializable]
    public class AlbumPageView
    {
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI ContentText;
        public Image PageImage;
        public GameObject LockedOverlay;
    }

    [System.Serializable]
    public class AlbumPageData
    {
        public string Title;
        [TextArea] public string Content;
        public Sprite UnlockedSprite;
        public Sprite LockedSprite;
    }

    [Header("アルバムパネル")]
    [SerializeField] private GameObject _albumPanel;

    [Header("ページデータ (インデックス = アルバムページ番号)")]
    [SerializeField] private AlbumPageData[] _pageData;

    [Header("ページUIビュー (インデックスを揃えること)")]
    [SerializeField] private AlbumPageView[] _pageViews;

    private void Start()
    {
        _albumPanel?.SetActive(false);
    }

    public void OpenAlbum()
    {
        Refresh();
        _albumPanel?.SetActive(true);
    }

    public void CloseAlbum()
    {
        _albumPanel?.SetActive(false);
    }

    private void Refresh()
    {
        var mgr = GameProgressManager.Instance;
        int count = Mathf.Min(
            _pageData  != null ? _pageData.Length  : 0,
            _pageViews != null ? _pageViews.Length : 0);

        for (int i = 0; i < count; i++)
        {
            bool unlocked = mgr != null && i < GameProgressManager.AlbumPageCount && mgr.AlbumPages[i];
            var  view     = _pageViews[i];
            var  data     = _pageData[i];

            if (view.LockedOverlay != null)
                view.LockedOverlay.SetActive(!unlocked);

            if (view.TitleText != null)
                view.TitleText.text = unlocked ? data.Title : "???";

            if (view.ContentText != null)
                view.ContentText.text = unlocked ? data.Content : "不明";

            if (view.PageImage != null)
            {
                var sprite = unlocked ? data.UnlockedSprite : data.LockedSprite;
                if (sprite != null) view.PageImage.sprite = sprite;
            }
        }
    }
}
