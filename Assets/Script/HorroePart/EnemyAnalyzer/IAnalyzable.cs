/// <summary>
/// 解析対象の敵が持つべきインターフェース。
/// EnemyDetector はこれを通じて敵の種別・ステータスを取得する。
/// </summary>
public interface IAnalyzable
{
    int Age { get; }
    string Gender { get; }
    float Height { get; }
    float BodyWeight { get; }
    string Condition { get; }
}