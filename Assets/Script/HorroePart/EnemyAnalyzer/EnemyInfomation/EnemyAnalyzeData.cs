/// <summary>
/// 個体ごとの解析状態を保持するデータクラス。
/// EnemyAnalyzer の Dictionary のバリューとして使用する。
/// </summary>
public class EnemyAnalyzeData
{
    public float AnalyzePercent = 0f;
    public int CurrentRevealIndex = 0;
    public string[] FieldValues = new string[5];
    public bool IsComplete => AnalyzePercent >= 100f;
}