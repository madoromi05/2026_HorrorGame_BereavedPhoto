using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EnemyAnalyzer から受け取った解析率をもとに各UIパーツを更新する。
/// フェードや項目開示のタイミングは Inspector から変更可能。
/// </summary>
public class AnalyzerUI : MonoBehaviour
{
    // ---- バー ----
    [SerializeField] private Image _barFill;

    private bool isRevealing = false;

    private void Awake()
    {
        if (_barFill == null)
            DebugCustom.LogError($"[AnalyzerUI] _barFill が未設定です。", this);
    }

    public void OnAnalyzeUpdate(float pct)
    {
        UpdateBar(pct);
        UpdateFields(pct);
    }

    private void UpdateBar(float pct)
    {
        _barFill.fillAmount = pct / 100f;
    }

    // フィールドのタイプライターを管理
    private void UpdateFields(float pct)
    {
        if (isRevealing) return;
        isRevealing = true;
    }

    public void ResetFields()
    {
        StopAllCoroutines();
        isRevealing = false;
    }
}