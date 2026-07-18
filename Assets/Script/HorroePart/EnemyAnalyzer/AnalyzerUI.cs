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
    // ---- 状態表示UI ----
    [SerializeField] private GameObject _analyzingIcon;      // 解析中に表示するUI
    [SerializeField] private GameObject _completeUI;         // 解析完了時に表示するUI

    private bool isRevealing = false;

    private void Awake()
    {
        if (_barFill == null)
            DebugCustom.LogError($"[AnalyzerUI] _barFill が未設定です。", this);

        // 初期状態は非表示にする
        if (_barFill != null) _barFill.fillAmount = 0f;   // バーを0%で初期化（構えるまで更新されないため）
        if (_analyzingIcon != null) _analyzingIcon.SetActive(false);
        if (_completeUI != null) _completeUI.SetActive(false);
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
        SetAnalyzingState(false);
        SetCompleteState(false);
    }

    /// 解析中アイコンの表示切替
    public void SetAnalyzingState(bool isAnalyzing)
    {
        if (_analyzingIcon != null && _analyzingIcon.activeSelf != isAnalyzing)
            _analyzingIcon.SetActive(isAnalyzing);
    }

    /// 解析完了UIの表示切替
    public void SetCompleteState(bool isComplete)
    {
        if (_completeUI != null && _completeUI.activeSelf != isComplete)
            _completeUI.SetActive(isComplete);
    }
}