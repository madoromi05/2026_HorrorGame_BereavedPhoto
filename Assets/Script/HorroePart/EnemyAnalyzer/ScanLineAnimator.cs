using UnityEngine;

/// <summary>
/// スキャンラインを上端から下端へ一定速度で移動させ、ループする。
/// CameraFrame の RectTransform を基準に上端・下端を算出するため、
/// フレームサイズを変更しても自動追従する。
/// </summary>
public class ScanLineAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform scanLine;
    [SerializeField] private RectTransform cameraFrame;

    [Header("アニメーション設定")]
    [SerializeField] private float speed = 180f;  // px/秒
    [SerializeField] private float fadeOutRange = 20f;  // 下端 n px 手前からフェードアウト

    private CanvasGroup m_canvasGroup;
    private float m_topY;
    private float m_bottomY;

    private void Awake()
    {
        // フェードに CanvasGroup を使う（なければ追加）
        m_canvasGroup = scanLine.GetComponent<CanvasGroup>();
        if (m_canvasGroup == null)
            m_canvasGroup = scanLine.gameObject.AddComponent<CanvasGroup>();

        CalcBounds();
        scanLine.anchoredPosition = new Vector2(0f, m_topY);
    }

    private void Update()
    {
        float y = scanLine.anchoredPosition.y - speed * Time.deltaTime;

        // 下端を超えたら上端に戻す
        if (y < m_bottomY)
            y = m_topY;

        scanLine.anchoredPosition = new Vector2(0f, y);

        // 下端に近づくほどフェードアウト
        float distToBottom = y - m_bottomY;
        m_canvasGroup.alpha = Mathf.Clamp01(distToBottom / fadeOutRange);
    }

    /// <summary>
    /// cameraFrame の高さを基準に上端・下端の Y 座標を計算する。
    /// アンカーを中央基準にしているため、上端は +height/2、下端は -height/2。
    /// </summary>
    private void CalcBounds()
    {
        float halfH = cameraFrame.rect.height * 0.5f;
        m_topY = halfH;
        m_bottomY = -halfH;
    }
}