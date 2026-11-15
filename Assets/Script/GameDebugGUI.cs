using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ボタンで開閉するデバッグオーバーレイ。
/// Canvas 上に配置し、_toggleButton に UI Button をアサインして使用する。
/// DontDestroyOnLoad のためシーンをまたいで動作する。
/// ウィンドウの枠とタブバーだけを描き、選ばれたタブの中身は IDebugTab の実装（Assets/Script/Debug/）に描かせる
/// </summary>
public class GameDebugGUI : MonoBehaviour
{
    private static GameDebugGUI _instance;

    [Header("トグルボタン")]
    [Tooltip("クリックで開閉する UI Button をアサイン")]
    [SerializeField] private Button _toggleButton;

    [Header("表示設定")]
    [SerializeField] private bool _showOnStart = true;

    private bool    _isVisible;
    private Vector2 _scroll;
    private Rect    _windowRect = new Rect(10, 10, 400, 580);
    private int     _selectedTab;

    private readonly DebugGUIContext _context = new DebugGUIContext();	// タブが読むシーン上の参照
    private readonly DebugGUIStyles  _styles  = new DebugGUIStyles();

    private readonly IDebugTab[] _tabs =	// タブバーの並び順はここだけで決まる
    {
        new ProgressDebugTab(),
        new PlayerDebugTab(),
        new EnemyDebugTab(),
        new SceneDebugTab(),
    };

    private float _refreshTimer;
    private const float kRefreshInterval = 0.5f;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _context.Clear();
    }

    private void Start()
    {
        _isVisible = _showOnStart;
        _toggleButton?.onClick.AddListener(ToggleVisibility);
        _context.Refresh();
    }

    private void OnDestroy()
    {
        _toggleButton?.onClick.RemoveListener(ToggleVisibility);
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void ToggleVisibility() => _isVisible = !_isVisible;

    private void Update()
    {
        _refreshTimer -= Time.deltaTime;
        if (_refreshTimer <= 0f)
        {
            _context.Refresh();
            _refreshTimer = kRefreshInterval;
        }
    }

    private void OnGUI()
    {
        if (!_isVisible) return;
        _styles.Build();

        _windowRect = GUI.Window(12345, _windowRect, DrawWindow, "■ Debug GUI");
    }

    private void DrawWindow(int id)
    {
        // タブバー
        GUILayout.BeginHorizontal();
        for (int i = 0; i < _tabs.Length; i++)
        {
            if (GUILayout.Button(_tabs[i].Title, _styles.TabStyle(i == _selectedTab)))
                _selectedTab = i;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        _scroll = GUILayout.BeginScrollView(_scroll);

        _tabs[_selectedTab].Draw(_context, _styles);

        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0, 0, 10000, 18));
    }
}
