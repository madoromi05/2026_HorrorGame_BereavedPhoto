using HorrorGame.Item;
using UnityEngine;

/// <summary>
/// デバッグ GUI のタブが読む、シーン上の参照の置き場所
/// GameDebugGUI が 1 つ持ち、一定間隔で Refresh()、シーンが読み込まれたら Clear() を呼ぶ。タブは Draw の引数で受け取って読む
/// 値の表示や参照先の操作はしない（タブの仕事）
/// </summary>
public class DebugGUIContext
{
    private EnemyAnalyzer        _analyzer;
    private Inventory            _inventory;
    private PlayerMover          _mover;
    private PlayerBreath         _breath;
    private PlayerDashController _dash;
    private HandLightController  _light;
    private GameOverHandler      _gameOverHandler;

    public EnemyAnalyzer        Analyzer        => _analyzer;
    public Inventory            Inventory       => _inventory;
    public PlayerMover          Mover           => _mover;
    public PlayerBreath         Breath          => _breath;
    public PlayerDashController Dash            => _dash;
    public GameOverHandler      GameOverHandler => _gameOverHandler;

    /// <summary>
    /// まだ見つかっていない参照だけをシーンから探す
    /// </summary>
    public void Refresh()
    {
        _analyzer ??= Object.FindFirstObjectByType<EnemyAnalyzer>();
        _inventory ??= Object.FindFirstObjectByType<Inventory>();
        _mover     ??= Object.FindFirstObjectByType<PlayerMover>();
        _breath    ??= Object.FindFirstObjectByType<PlayerBreath>();
        _dash      ??= Object.FindFirstObjectByType<PlayerDashController>();
        _light     ??= Object.FindFirstObjectByType<HandLightController>();
        _gameOverHandler ??= Object.FindFirstObjectByType<GameOverHandler>();	// ゲームオーバー発生後は自身を enabled=false にするため、取り直せるうちにキャッシュしておく
    }

    /// <summary>
    /// 前のシーンの参照を捨てる。次の Refresh() で探し直す
    /// </summary>
    public void Clear()
    {
        _analyzer    = null;
        _inventory   = null;
        _mover       = null;
        _breath      = null;
        _dash        = null;
        _light       = null;
        _gameOverHandler = null;
    }
}
