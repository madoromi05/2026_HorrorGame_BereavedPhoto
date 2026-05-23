using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// 指定したTextMeshProラベル群をランダムな16進数・小数でスクランブル更新する。
/// updateInterval を Inspector から調整可能。
/// </summary>
public class NoiseTextScrambler : MonoBehaviour
{
    [SerializeField] private TMP_Text[] noiseLabels;
    [SerializeField] private float updateInterval = 0.12f;  // 秒

    private const string kHexChars = "0123456789ABCDEF";

    private IEnumerator Start()
    {
        while (true)
        {
            foreach (var label in noiseLabels)
            {
                if (label == null) continue;
                label.text = Random.value > 0.4f
                    ? $"{RandHex(4)}:{RandHex(2)}"
                    : $"{Random.Range(0, 1000):D3}.{Random.Range(0, 10000):D4}";
            }
            yield return new WaitForSeconds(updateInterval);
        }
    }

    private string RandHex(int length)
    {
        var sb = new System.Text.StringBuilder(length);
        for (int i = 0; i < length; i++)
            sb.Append(kHexChars[Random.Range(0, kHexChars.Length)]);
        return sb.ToString();
    }
}