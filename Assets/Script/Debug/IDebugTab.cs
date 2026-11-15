/// <summary>
/// デバッグ GUI の 1 タブ
/// GameDebugGUI がタブバーの順に並べて持ち、選ばれているタブの Draw だけをスクロール領域の中で呼ぶ。タブどうしは互いを知らない
/// ウィンドウの枠・タブバー・スクロールはタブ側で扱わない
/// </summary>
public interface IDebugTab
{
    string Title { get; }	// タブバーのボタンに出す名前

    void Draw(DebugGUIContext context, DebugGUIStyles gui);
}
