/// <summary>
/// ログ出力の契約を定義するインターフェース。
/// リリースビルドでの出力制御は実装クラスが責任を持つ。
/// </summary>
public interface IDebugLogger
{
    void Log(string message, UnityEngine.Object context = null);
    void LogError(string message, UnityEngine.Object context = null);
    void LogWarning(string message, UnityEngine.Object context = null);
    void LogException(System.Exception exception, UnityEngine.Object context = null);
}