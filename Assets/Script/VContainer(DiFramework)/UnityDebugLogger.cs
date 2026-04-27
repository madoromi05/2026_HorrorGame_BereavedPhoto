/// <summary>
/// UnityEngine.Debugへ出力するIDebugLoggerの実装。
/// DEBUGシンボルが有効な場合のみ出力する。
/// </summary>
public sealed class UnityDebugLogger : IDebugLogger
{
    public void Log(string message, UnityEngine.Object context = null)
    {
#if DEBUG
        global::UnityEngine.Debug.Log(message, context);
#endif
    }

    public void LogError(string message, UnityEngine.Object context = null)
    {
#if DEBUG
        global::UnityEngine.Debug.LogError(message, context);
#endif
    }

    public void LogWarning(string message, UnityEngine.Object context = null)
    {
#if DEBUG
        global::UnityEngine.Debug.LogWarning(message, context);
#endif
    }

    public void LogException(System.Exception exception, UnityEngine.Object context = null)
    {
#if DEBUG
        global::UnityEngine.Debug.LogException(exception, context);
#endif
    }
}