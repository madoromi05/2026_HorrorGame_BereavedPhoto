using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// EnemyAnalyzer から受け取った解析率をもとに各UIパーツを更新する。
/// フェードや項目開示のタイミングは Inspector から変更可能。
/// </summary>
public class AnalyzerUI : MonoBehaviour
{
    // ---- バー ----
    [SerializeField] private Image _barFill;

    // ---- フィールド開示 ----
    [SerializeField] private float _typewriterInterval = 10f;

    private int currentRevealIndex = 0;
    private bool isRevealing = false;

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

    /// <summary>
    /// テキストを1文字ずつ表示するタイプライター演出。
    /// charInterval で1文字あたりの表示間隔を調整できる。
    /// </summary>
    private IEnumerator TypewriterReveal(TMP_Text label, string fullText)
    {
        label.text = "";
        foreach (char c in fullText)
        {
            label.text += c;
            yield return new WaitForSeconds(_typewriterInterval);
        }

        currentRevealIndex++;
        isRevealing = false;
    }

    public void ResetFields()
    {
        StopAllCoroutines();
        currentRevealIndex = 0;
        isRevealing = false;
    }
}