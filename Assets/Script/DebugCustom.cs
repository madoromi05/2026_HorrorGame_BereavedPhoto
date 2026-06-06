using UnityEngine;
using System.Diagnostics;
/// <summary>
/// エディタ実行時のみログを表示します。
/// </summary>
public static class DebugCustom
{
    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void Log(string message, UnityEngine.Object context = null)
    {
        global::UnityEngine.Debug.Log(message, context);
    }

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void LogError(string message, UnityEngine.Object context = null)
    {
        global::UnityEngine.Debug.LogError(message, context);
    }

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void LogWarning(string message, UnityEngine.Object context = null)
    {
        global::UnityEngine.Debug.LogWarning(message, context);
    }

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void LogException(System.Exception exception, UnityEngine.Object context = null)
    {
        global::UnityEngine.Debug.LogException(exception, context);
    }

    /// <summary>
    /// 複数の必須 [SerializeField] 参照をまとめて検証する（2変数以上用）。
    /// null のフィールドごとに「[クラス名] <フィールド名> が未設定です」を LogError 出力する。
    /// </summary>

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void ValidateFields(UnityEngine.Object owner, params (string name, UnityEngine.Object value)[] fields)
    {
        foreach (var (name, value) in fields)
        {
            if (value == null)
                LogError($"[{owner.GetType().Name}] {name} が未設定です。", owner);
        }
    }
}