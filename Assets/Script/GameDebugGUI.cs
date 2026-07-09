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

    // スタイル/テクスチャ
    private GUIStyle _headerStyle;
    private GUIStyle _valueStyle;
    private GUIStyle _tabActiveStyle;
    private GUIStyle _tabStyle;
    private Texture2D _barBgTex;
    private Texture2D _barGreenTex;
    private Texture2D _barYellowTex;
    private Texture2D _barRedTex;
    private Texture2D _windowBgTex;
    private bool      _stylesBuilt;

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

        _windowBgTex  = MakeTex(1, 1, new Color(0.08f, 0.08f, 0.12f, 0.94f));
        _barBgTex     = MakeTex(1, 1, new Color(0.2f, 0.2f, 0.2f, 1f));
        _barGreenTex  = MakeTex(1, 1, new Color(0.15f, 0.75f, 0.3f, 1f));
        _barYellowTex = MakeTex(1, 1, new Color(0.9f, 0.75f, 0.1f, 1f));
        _barRedTex    = MakeTex(1, 1, new Color(0.9f, 0.2f, 0.2f, 1f));

        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize  = 12,
            normal    = { textColor = new Color(0.5f, 0.85f, 1f) }
        };

        _valueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal   = { textColor = Color.white }
        };

        _tabStyle = new GUIStyle(GUI.skin.button);
        _tabActiveStyle = new GUIStyle(GUI.skin.button)
        {
            normal  = { background = MakeTex(1, 1, new Color(0.25f, 0.5f, 0.85f, 1f)),
                        textColor  = Color.white },
            focused = { background = MakeTex(1, 1, new Color(0.25f, 0.5f, 0.85f, 1f)),
                        textColor  = Color.white },
        };

        var winStyle = GUI.skin.window;
        winStyle.normal.background = _windowBgTex;

        _stylesBuilt = true;
    }

    private void OnGUI()
    {
        if (!_isVisible) return;
        BuildStyles();

        GUI.backgroundColor = new Color(0.08f, 0.08f, 0.14f, 0.95f);
        _windowRect = GUI.Window(12345, _windowRect, DrawWindow, "■ Debug GUI");
        GUI.backgroundColor = Color.white;
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
        var mgr = GameProgressManager.Instance;

        SectionHeader("ゲームステージ");

        if (mgr == null)
        {
            Label("GameProgressManager: 未検出", Color.red);
        }
        else
        {
            // フロー図
            var stages = (GameProgressManager.GameStage[])System.Enum.GetValues(typeof(GameProgressManager.GameStage));
            GUILayout.BeginHorizontal();
            foreach (var s in stages)
            {
                bool cur = mgr.CurrentStage == s;
                GUI.color = cur ? new Color(0.3f, 1f, 0.5f) : Color.gray;
                GUILayout.Label(cur ? $"[{s}]" : $" {s} ");
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            Label($"現在ステージ : {mgr.CurrentStage}", Color.white);
            Label($"次シーン      : {mgr.GetSceneForStage(mgr.CurrentStage)}", Color.cyan);

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("▶ 次へ進む"))  { mgr.LoadNextScene(); }
            if (GUILayout.Button("↺ リセット")) { mgr.ResetProgress(); DebugCustom.Log("[Debug] 進行リセット"); }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(8);
        SectionHeader("アルバムページ");

        string[] pageNames = { "0:表紙", "1:母", "2:父", "3:終章" };
        for (int i = 0; i < GameProgressManager.AlbumPageCount; i++)
        {
            bool unlocked = mgr != null && mgr.AlbumPages[i];
            GUILayout.BeginHorizontal();
            Label(unlocked ? $"[✓] {pageNames[i]}" : $"[  ] {pageNames[i]}",
                  unlocked ? new Color(0.3f, 1f, 0.4f) : Color.gray);

            if (!unlocked && mgr != null)
            {
                if (GUILayout.Button("解放", GUILayout.Width(44)))
                    mgr.UnlockAlbumPage(i);
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(8);
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

        // ---- ステージジャンプ（ステージ状態も正しく設定） ----
        GUILayout.Space(4);
        Label("ステージジャンプ（ステージも更新）:", Color.gray);

        GUILayout.BeginHorizontal();
        DrawStageButton("Title",    GameProgressManager.GameStage.Title,    mgr);
        DrawStageButton("Horror",   GameProgressManager.GameStage.Horror,   mgr);
        DrawStageButton("Epilogue", GameProgressManager.GameStage.Epilogue, mgr);
        GUILayout.EndHorizontal();

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

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("解析100%完了"))
        {
            if (_analyzer != null) _analyzer.DebugForceComplete();
            else DebugCustom.LogWarning("[Debug] EnemyAnalyzer not found");
        }
        if (GUILayout.Button("鍵 全取得"))
        {
            if (_inventory != null)
            {
                foreach (ItemType t in System.Enum.GetValues(typeof(ItemType)))
                    _inventory.DebugAddItem(t);
            }
            else DebugCustom.LogWarning("[Debug] Inventory not found");
        }
        GUILayout.EndHorizontal();

        if (mgr != null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("アルバム全解放"))
            {
                for (int i = 0; i < GameProgressManager.AlbumPageCount; i++)
                    mgr.UnlockAlbumPage(i);
            }
            if (GUILayout.Button("進行リセット"))
            {
                mgr.ResetProgress();
                DebugCustom.Log("[Debug] 進行リセット");
            }
            GUILayout.EndHorizontal();
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

    private void DrawStageButton(string label, GameProgressManager.GameStage stage, GameProgressManager mgr)
    {
        bool isCurrent = mgr != null && mgr.CurrentStage == stage;
        var  prev      = GUI.enabled;
        GUI.enabled = !isCurrent;
        GUI.backgroundColor = isCurrent ? new Color(0.3f, 0.6f, 1f) : Color.white;
        if (GUILayout.Button(isCurrent ? $"▶{label}" : label))
            mgr?.DebugJumpToStage(stage);
        GUI.backgroundColor = Color.white;
        GUI.enabled = prev;
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

        float pct = _analyzer.AnalyzePercent;
        bool done = _analyzer.IsComplete;

        Label($"解析率: {pct:F1}%  {(done ? "✓ 完了" : "進行中")}",
              done ? new Color(0.3f, 1f, 0.4f) : Color.white);
        DrawBar(pct, 100f, done ? _barGreenTex : (pct > 50f ? _barYellowTex : _barBgTex));
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

    private void DrawBar(float value, float max, Texture2D fillTex = null)
    {
        float ratio = Mathf.Clamp01(max > 0f ? value / max : 0f);
        Rect bg = GUILayoutUtility.GetRect(0, 12, GUILayout.ExpandWidth(true));

        if (_barBgTex != null) GUI.DrawTexture(bg, _barBgTex);
        else GUI.Box(bg, GUIContent.none);

        if (ratio > 0f)
        {
            var fill = new Rect(bg.x, bg.y, bg.width * ratio, bg.height);
            Texture2D tex = fillTex ?? _barGreenTex;
            if (tex != null) GUI.DrawTexture(fill, tex);
            else GUI.Box(fill, GUIContent.none);
        }
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

    private static Texture2D MakeTex(int w, int h, Color c)
    {
        var pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = c;
        var t = new Texture2D(w, h);
        t.SetPixels(pix);
        t.Apply();
        return t;
    }
}
