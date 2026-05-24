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
    [SerializeField] private Image barFill;
    [SerializeField] private TMP_Text barPercentText;

    // ---- ノイズパネル（左） ----
    [SerializeField] private CanvasGroup noisePanelGroup;
    [SerializeField] private float noiseRevealAt = 10f;  // %
    [SerializeField] private float noiseFadeRange = 10f;  // フェード幅

    // ---- 敵情報ウィンドウ（右） ----
    [SerializeField] private CanvasGroup infoWindowGroup;
    [SerializeField] private float infoRevealAt = 25f;
    [SerializeField] private float infoFadeRange = 15f;

    // ---- フィールド開示 ----
    [SerializeField] private float typewriterInterval = 10f;
    [Serializable]
    private class FieldRevealEntry
    {
        public TMP_Text label;
        public string value;
        public float revealAt;
    }
    [SerializeField] private FieldRevealEntry[] fieldEntries;

    private int currentRevealIndex = 0;
    private bool isRevealing = false;

    public void OnAnalyzeUpdate(float pct)
    {
        UpdateBar(pct);
        UpdateFade(noisePanelGroup, pct, noiseRevealAt, noiseFadeRange);
        UpdateFade(infoWindowGroup, pct, infoRevealAt, infoFadeRange);
        UpdateFields(pct);
    }

    private void UpdateBar(float pct)
    {
        barFill.fillAmount = pct / 100f;
        barPercentText.text = $"{Mathf.RoundToInt(pct)}%";
    }

    // ノイズパネル・情報ウィンドウをフェードインさせる共通処理
    private void UpdateFade(CanvasGroup group, float pct, float revealAt, float fadeRange)
    {
        if (group == null) return;
        float alpha = Mathf.Clamp01((pct - revealAt) / fadeRange);
        group.alpha = alpha;
        group.blocksRaycasts = alpha > 0f;
    }

    // フィールドのタイプライターを管理
    private void UpdateFields(float pct)
    {
        if (isRevealing || currentRevealIndex >= fieldEntries.Length) return;

        var entry = fieldEntries[currentRevealIndex];
        if (entry.label == null || pct < entry.revealAt) return;

        isRevealing = true;
        StartCoroutine(TypewriterReveal(entry.label, entry.value));
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
            yield return new WaitForSeconds(typewriterInterval);
        }

        currentRevealIndex++;
        isRevealing = false;
    }

    /// <summary>
    /// 検知した敵のデータをフィールド開示用にキャッシュする。
    /// 解析が進むにつれて OnAnalyzeUpdate 内で段階的に表示される。
    /// </summary>
    public void SetEnemyData(IAnalyzable data)
    {
        // fieldEntries の value を動的に上書きする
        // 配列インデックスは Inspector の並び順と対応させる
        if (fieldEntries.Length < 5) return;

        fieldEntries[0].value = data.Age.ToString();
        fieldEntries[1].value = data.Gender;
        fieldEntries[2].value = $"{data.Height} cm";
        fieldEntries[3].value = $"{data.BodyWeight} kg";
        fieldEntries[4].value = data.Condition;
    }

    public void ResetFields()
    {
        StopAllCoroutines();
        currentRevealIndex = 0;
        isRevealing = false;
        foreach (var entry in fieldEntries)
        {
            if (entry.label != null)
                entry.label.text = "?";
        }
    }
}