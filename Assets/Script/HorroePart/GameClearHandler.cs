using System.Collections;
using UnityEngine;

/// <summary>
/// 全種類の敵の解析完了を監視し、完了していればクリア（次シーン）へ遷移させるコンポーネント。
/// 「解析が完了したか」の判定は EnemyAnalyzer.IsComplete に委ね、
/// このクラスは「完了を検知して待機し、次シーンへ遷移する」というクリア遷移ポリシーだけを担う。
/// （EnemyAnalyzer からシーン遷移の知識を切り離すために分離した。）
///
/// EnemyAnalyzer とは別オブジェクトに置ける。_analyzer 未割当ならシーンから自動解決する。
/// </summary>
public class GameClearHandler : MonoBehaviour
{
    [Tooltip("別オブジェクトの EnemyAnalyzer を割り当てる。未割当なら Awake でシーンから自動取得する。")]
    [SerializeField] private EnemyAnalyzer _analyzer;

    [Header("クリア遷移")]
    [SerializeField] private bool  _autoTransitionToClear = true;
    [Tooltip("解析完了からクリア遷移までの待機秒数")]
    [SerializeField] private float _clearTransitionDelay = 1.0f;

    // 完了確認は毎フレームである必要がないため一定間隔で行う。
    private const float kClearCheckInterval = 0.5f;
    private float _clearCheckTimer;
    private bool  _clearTriggered;

    private void Awake()
    {
        // 別オブジェクト配置を前提に、未割当ならシーンから取得する。
        if (_analyzer == null)
            _analyzer = FindFirstObjectByType<EnemyAnalyzer>();

        DebugCustom.ValidateFields(this, (nameof(_analyzer), _analyzer));
    }

    private void Update()
    {
        if (!_autoTransitionToClear || _clearTriggered) return;

        _clearCheckTimer -= Time.deltaTime;
        if (_clearCheckTimer > 0f) return;
        _clearCheckTimer = kClearCheckInterval;

        if (_analyzer.IsComplete)
        {
            _clearTriggered = true;
            StartCoroutine(TransitionToClearCoroutine());
        }
    }

    // 解析完了後、少し待ってからクリア（次シーン）へ遷移する。
    private IEnumerator TransitionToClearCoroutine()
    {
        DebugCustom.Log("[GameClearHandler] 全敵の解析完了。クリアへ遷移します。", this);
        yield return new WaitForSeconds(_clearTransitionDelay);

        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.LoadNextScene();
        else
            DebugCustom.LogWarning("[GameClearHandler] GameProgressManager が見つかりません。クリア遷移できません。", this);
    }
}
