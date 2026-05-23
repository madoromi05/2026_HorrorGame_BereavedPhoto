using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [Serializable]
    private struct FieldRevealEntry
    {
        public TMP_Text label;
        public string value;
        public float revealAt;   // この%を超えたら表示
    }
    [SerializeField] private FieldRevealEntry[] fieldEntries;

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

    private void UpdateFade(CanvasGroup group, float pct, float revealAt, float fadeRange)
    {
        if (group == null) return;
        float alpha = Mathf.Clamp01((pct - revealAt) / fadeRange);
        group.alpha = alpha;
        group.blocksRaycasts = alpha > 0f;
    }

    private void UpdateFields(float pct)
    {
        foreach (var entry in fieldEntries)
        {
            if (entry.label == null) continue;
            entry.label.text = pct >= entry.revealAt ? entry.value : "?";
        }
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
}