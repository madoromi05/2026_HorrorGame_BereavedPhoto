using UnityEngine;

/// <summary>
/// デバッグ GUI の見た目の係。スタイルを作って持ち、見出し・色付きラベル・タブボタンの見た目をそろえる
/// GameDebugGUI が 1 つ持ち、OnGUI の最初に Build() を呼ぶ。タブは Draw の引数で受け取って描くのに使う
/// 何を描くか（ウィンドウの枠・タブの中身）は決めない
/// </summary>
public class DebugGUIStyles
{
    public static readonly Color kDoneColor = new Color(0.3f, 1f, 0.4f);	// 完了・所持・無敵 ON を示す緑

    private const int kHeaderFontSize = 12;
    private const int kValueFontSize  = 11;

    private GUIStyle _headerStyle;
    private GUIStyle _valueStyle;
    private GUIStyle _tabActiveStyle;
    private GUIStyle _tabStyle;
    private bool     _stylesBuilt;

    /// <summary>
    /// GUI.skin を元にスタイルを作る。GUI.skin は OnGUI の中でしか読めないため、OnGUI から呼ぶ。2 回目以降は何もしない
    /// </summary>
    public void Build()
    {
        if (_stylesBuilt) return;

        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize  = kHeaderFontSize,
        };

        _valueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = kValueFontSize,
        };

        _tabStyle = new GUIStyle(GUI.skin.button);
        _tabActiveStyle = new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold,	// アクティブタブは色ではなく太字で示す（色装飾を持たせない）
        };

        _stylesBuilt = true;
    }

    public GUIStyle TabStyle(bool isActive) => isActive ? _tabActiveStyle : _tabStyle;

    public void SectionHeader(string text)
    {
        if (_headerStyle == null) Build();
        GUILayout.Label(text, _headerStyle);
    }

    public void Label(string text, Color color)
    {
        var prev = GUI.color;
        GUI.color = color;
        GUILayout.Label(text, _valueStyle ?? GUI.skin.label);
        GUI.color = prev;
    }
}
