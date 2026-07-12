using HorrorGame.Item;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ボタンで開閉するデバッグオーバーレイ。
/// Canvas 上に配置し、_toggleButton に UI Button をアサインして使用する。
/// DontDestroyOnLoad のためシーンをまたいで動作する。
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

    // キャッシュ
    private EnemyAnalyzer      _analyzer;
    private Inventory          _inventory;
    private PlayerMover        _mover;
    private PlayerBreath       _breath;
    private PlayerDashController _dash;
    private HandLightController  _light;
    private EnemyController[]    _enemies;

    private float _refreshTimer;
    private const float kRefreshInterval = 0.5f;

    // スタイル
    private GUIStyle _headerStyle;
    private GUIStyle _valueStyle;
    private GUIStyle _tabActiveStyle;
    private GUIStyle _tabStyle;
    private bool     _stylesBuilt;

    private static readonly string[] kTabNames = { "進行", "プレイヤー", "敵", "シーン" };

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
        _analyzer    = null;
        _inventory   = null;
        _mover       = null;
        _breath      = null;
        _dash        = null;
        _light       = null;
        _enemies     = null;
    }

    private void Start()
    {
        _isVisible = _showOnStart;
        _toggleButton?.onClick.AddListener(ToggleVisibility);
        RefreshRefs();
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
            RefreshRefs();
            _refreshTimer = kRefreshInterval;
        }
    }

    private void RefreshRefs()
    {
        _analyzer ??= FindFirstObjectByType<EnemyAnalyzer>();
        _inventory ??= FindFirstObjectByType<Inventory>();
        _mover     ??= FindFirstObjectByType<PlayerMover>();
        _breath    ??= FindFirstObjectByType<PlayerBreath>();
        _dash      ??= FindFirstObjectByType<PlayerDashController>();
        _light     ??= FindFirstObjectByType<HandLightController>();
        _enemies   = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
    }

    private void BuildStyles()
    {
        if (_stylesBuilt) return;

        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize  = 12,
        };

        _valueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
        };

        // アクティブタブは色ではなく太字で示す（色装飾を持たせない）。
        _tabStyle = new GUIStyle(GUI.skin.button);
        _tabActiveStyle = new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold,
        };

        _stylesBuilt = true;
    }

    private void OnGUI()
    {
        if (!_isVisible) return;
        BuildStyles();

        _windowRect = GUI.Window(12345, _windowRect, DrawWindow, "■ Debug GUI");
    }

    private void DrawWindow(int id)
    {
        // タブバー
        GUILayout.BeginHorizontal();
        for (int i = 0; i < kTabNames.Length; i++)
        {
            var style = i == _selectedTab ? _tabActiveStyle : _tabStyle;
            if (GUILayout.Button(kTabNames[i], style))
                _selectedTab = i;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        _scroll = GUILayout.BeginScrollView(_scroll);

        switch (_selectedTab)
        {
            case 0: DrawTabProgress(); break;
            case 1: DrawTabPlayer();   break;
            case 2: DrawTabEnemies();  break;
            case 3: DrawTabScene();    break;
        }

        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0, 0, 10000, 18));
    }

    // ================================================================
    //  タブ 0: ゲーム進行
    // ================================================================

    private void DrawTabProgress()
    {
        SectionHeader("解析状況");
        DrawAnalyzerSection();
    }

    // ================================================================
    //  タブ 1: プレイヤー
    // ================================================================

    private void DrawTabPlayer()
    {
        // ---- デバッグ操作（無敵・速度変更） ----
        SectionHeader("デバッグ");

        GUILayout.BeginHorizontal();
        Label("無敵:", Color.white);
        Label(GameOverHandler.DebugInvincible ? "ON" : "OFF",
              GameOverHandler.DebugInvincible ? new Color(0.3f, 1f, 0.4f) : Color.gray);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(GameOverHandler.DebugInvincible ? "無敵 OFF" : "無敵 ON", GUILayout.Width(90)))
            GameOverHandler.DebugInvincible = !GameOverHandler.DebugInvincible;
        GUILayout.EndHorizontal();

        if (_mover != null)
        {
            Label($"移動速度: {_mover.MoveSpeed:F0}", Color.white);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("10"))  _mover.SetMoveSpeed(10f);
            if (GUILayout.Button("25"))  _mover.SetMoveSpeed(25f);
            if (GUILayout.Button("50")) _mover.SetMoveSpeed(50f);
            GUILayout.EndHorizontal();
        }
        else Label("PlayerMover: 未検出（速度変更不可）", Color.red);

        GUILayout.Space(6);
        SectionHeader("移動状態");
        if (_mover != null)
            Label($"MoveState: {_mover.CurrentMoveState}", Color.white);
        else
            Label("PlayerMover: 未検出", Color.red);

        GUILayout.Space(6);
        SectionHeader("ブレス");
        if (_breath != null)
        {
            Label($"息止め中: {_breath.IsHoldingBreath}",  Color.white);
            Label($"息切れ中: {_breath.IsGasping}",
                  _breath.IsGasping ? Color.red : Color.white);
        }
        else Label("PlayerBreath: 未検出", Color.gray);

        GUILayout.Space(6);
        SectionHeader("ダッシュ");
        if (_dash != null)
        {
            Label($"疲労中: {_dash.IsExhausted}",
                  _dash.IsExhausted ? Color.red : Color.white);
        }
        else Label("PlayerDashController: 未検出", Color.gray);

        GUILayout.Space(6);
        SectionHeader("インベントリ");
        if (_inventory != null)
        {
            foreach (ItemType t in System.Enum.GetValues(typeof(ItemType)))
            {
                bool has = _inventory.HasItem(t);
                Label(has ? $"[✓] {t}" : $"[  ] {t}",
                      has ? new Color(0.3f, 1f, 0.4f) : Color.gray);
            }
        }
        else Label("Inventory: 未検出", Color.red);
    }

    // ================================================================
    //  タブ 2: 敵
    // ================================================================

    private void DrawTabEnemies()
    {
        SectionHeader($"敵 ({(_enemies?.Length ?? 0)}体)");

        if (_enemies == null || _enemies.Length == 0)
        {
            Label("敵オブジェクトなし", Color.gray);
            return;
        }

        for (int i = 0; i < _enemies.Length; i++)
        {
            var e = _enemies[i];
            if (e == null) continue;

            var ghost = e.GetComponent<GhostIdentity>();
            string name = ghost != null ? $"[{ghost.GhostType}]" : $"[Enemy{i}]";
            bool activated = e.IsActivated;
            Color stateColor = activated ? Color.red : Color.white;

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            Label($"{name}  {(activated ? "追跡中" : "待機中")}", stateColor);
            GUILayout.FlexibleSpace();
            if (_analyzer != null && ghost != null)
            {
                // 解析率は種類（GhostType）ごとに共有される
                float ap = _analyzer.GetAnalyzePercent(ghost.GhostType);
                Label($"解析: {ap:F0}%", ap >= 100f ? new Color(0.3f, 1f, 0.4f) : Color.white);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(2);
        }
    }

    // ================================================================
    //  タブ 3: シーン
    // ================================================================

    private void DrawTabScene()
    {
        var mgr = GameProgressManager.Instance;
        string curScene = SceneManager.GetActiveScene().name;
        string curStage = mgr != null ? mgr.CurrentStage.ToString() : "N/A";

        SectionHeader($"シーン: {curScene}  |  ステージ: {curStage}");

        // ---- シーン直接遷移（ステージ変更なし） ----
        GUILayout.Space(6);
        Label("シーン直接遷移（ステージ変更なし）:", Color.gray);

        GUILayout.BeginHorizontal();
        DrawSceneButton("TitleScene",   GameProgressManager.SceneTitleName);
        DrawSceneButton("ScenarioPart", GameProgressManager.SceneStoryName);
        DrawSceneButton("PrepScene",    GameProgressManager.ScenePrepName);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        DrawSceneButton("HorrorScene", GameProgressManager.SceneHorrorName);
        DrawSceneButton("GameOver",    GameProgressManager.SceneGameOver);
        GUILayout.EndHorizontal();

        // ---- デバッグ操作 ----
        GUILayout.Space(6);
        SectionHeader("デバッグ操作");

        if (GUILayout.Button("鍵 全取得"))
        {
            if (_inventory != null)
            {
                foreach (ItemType t in System.Enum.GetValues(typeof(ItemType)))
                    _inventory.DebugAddItem(t);
            }
            else DebugCustom.LogWarning("[Debug] Inventory not found");
        }

        // ---- FPS / TimeScale ----
        GUILayout.Space(6);
        SectionHeader("FPS / TimeScale");
        Label($"{(1f / Mathf.Max(Time.deltaTime, 0.001f)):F0} fps    TimeScale: {Time.timeScale:F2}", Color.white);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("x0.5")) Time.timeScale = 0.5f;
        if (GUILayout.Button("x1.0")) Time.timeScale = 1f;
        if (GUILayout.Button("x2.0")) Time.timeScale = 2f;
        if (GUILayout.Button("x5.0")) Time.timeScale = 5f;
        GUILayout.EndHorizontal();
    }

    // ================================================================
    //  解析セクション（複数タブで共有）
    // ================================================================

    private void DrawAnalyzerSection()
    {
        if (_analyzer == null)
        {
            Label("EnemyAnalyzer: 未検出", Color.gray);
            return;
        }

        // 全体の完了判定（全種類が100%か）
        bool allDone = _analyzer.IsComplete;
        Label(allDone ? "全種類 ✓ 完了" : "解析中（未完了の種類あり）",
              allDone ? new Color(0.3f, 1f, 0.4f) : Color.white);

        GUILayout.Space(4);

        // 種類ごとに解析率を表示し、その種類だけを完了させる。
        foreach (EnemyType type in System.Enum.GetValues(typeof(EnemyType)))
        {
            float pct  = _analyzer.GetAnalyzePercent(type);
            bool  done = pct >= 100f;

            GUILayout.BeginHorizontal();
            Label($"{type}: {pct:F0}%", done ? new Color(0.3f, 1f, 0.4f) : Color.white);
            GUILayout.FlexibleSpace();
            var prev = GUI.enabled;
            GUI.enabled = !done;
            if (GUILayout.Button("完了", GUILayout.Width(60)))
                _analyzer.DebugCompleteType(type);
            GUI.enabled = prev;
            GUILayout.EndHorizontal();
        }
    }

    // ================================================================
    //  UI ヘルパー
    // ================================================================

    private void SectionHeader(string text)
    {
        if (_headerStyle == null) BuildStyles();
        GUILayout.Label(text, _headerStyle);
    }

    private void Label(string text, Color color)
    {
        var prev = GUI.color;
        GUI.color = color;
        GUILayout.Label(text, _valueStyle ?? GUI.skin.label);
        GUI.color = prev;
    }

    private void DrawSceneButton(string label, string sceneName)
    {
        bool isCurrent = SceneManager.GetActiveScene().name == sceneName;
        var prev = GUI.enabled;
        GUI.enabled = !isCurrent;
        if (GUILayout.Button(isCurrent ? $"▶ {label} (現在)" : label))
            SceneManager.LoadScene(sceneName);
        GUI.enabled = prev;
    }
}
