using UnityEngine;
using System.Diagnostics;
/// <summary>
/// エディタ実行時のみログを表示します。
/// </summary>
public static class DebugCustom
{
    [Conditional("DEBUG")]
    public static void Log(string message, UnityEngine.Object context = null)
    {
        global::UnityEngine.Debug.Log(message, context);
    }
    [Conditional("DEBUG")]
    public static void LogError(string message, UnityEngine.Object context = null)
    {
        global::UnityEngine.Debug.LogError(message, context);
    }
    [Conditional("DEBUG")]
    public static void LogWarning(string message, UnityEngine.Object context = null)
    {
        global::UnityEngine.Debug.LogWarning(message, context);
    }

    [Conditional("DEBUG")]
    public static void LogException(System.Exception exception, UnityEngine.Object context = null)
    {
        global::UnityEngine.Debug.LogException(exception, context);
    }
}