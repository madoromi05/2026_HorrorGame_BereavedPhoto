using System.Collections;
using UnityEngine;

/// <summary>
/// ホラーシーン開始時に真っ黒な状態から明転（フェードイン）させる演出コンポーネント。
/// 責務は「開始時の暗転維持」と「明転」のみで、シーン遷移やダンジョン生成の知識は持たない。
///
/// タイトル側の <see cref="TitleIntroDirector.Darken"/>（明→暗）と対になる暗→明の演出。
/// タイトルの暗転オーバーレイはシーン破棄で消えるため、遷移先のこのシーンで
/// 改めて真っ黒から始めることで「暗転を挟んでフェード開始」を成立させる。
///
/// 明転の起点はダンジョン生成完了（<see cref="DungeonGenerator.OnRoomPlaced"/>）に合わせる。
/// これにより将来生成を非同期化しても、生成過程を黒で隠したまま完了後に明転できる。
/// </summary>
public class ScreenFadeIn : MonoBehaviour
{
    [Header("オーバーレイ")]
    [Tooltip("明転用の全画面 CanvasGroup（黒 Image・alpha 1 開始）。")]
    [SerializeField] private CanvasGroup _darkenOverlay;

    [Header("トリガー")]
    [Tooltip("生成完了を待つ DungeonGenerator。未割当なら Awake でシーンから自動取得する。")]
    [SerializeField] private DungeonGenerator _dungeonGenerator;

    [Header("パラメータ")]
    [Tooltip("真っ黒（alpha 1）から表示（alpha 0）へ明転しきるまでの時間（秒）。")]
    [SerializeField] private float _fadeInDuration = 1.5f;

    private bool _fadeStarted; // 生成完了イベントの多重発火に対する二重起動防止

    private void Awake()
    {
        // 別オブジェクト配置を許容し、未割当ならシーンから取得する（GameClearHandler と同方針）。
        if (_dungeonGenerator == null)
            _dungeonGenerator = FindFirstObjectByType<DungeonGenerator>();

        DebugCustom.ValidateFields(this,
            (nameof(_darkenOverlay), _darkenOverlay),
            (nameof(_dungeonGenerator), _dungeonGenerator));

        // 初回描画フレームより前に真っ黒を確定させ、シーンの一瞬の映り込みを防ぐ。
        if (_darkenOverlay != null)
        {
            _darkenOverlay.alpha = 1f;
            _darkenOverlay.blocksRaycasts = true; // 明転しきるまで入力を遮断する
        }

        // DungeonGenerator.Start() での OnRoomPlaced 発火を取りこぼさないよう Awake で購読する。
        if (_dungeonGenerator != null)
            _dungeonGenerator.OnRoomPlaced += OnGenerationComplete;
    }

    private void OnDestroy()
    {
        if (_dungeonGenerator != null)
            _dungeonGenerator.OnRoomPlaced -= OnGenerationComplete;
    }

    // 生成完了を受けて明転を開始する。以後の発火は無視する。
    private void OnGenerationComplete(Transform _)
    {
        if (_fadeStarted)
            return;
        _fadeStarted = true;

        // 開始後は不要な購読を解除し、コルーチンで明転させる。
        _dungeonGenerator.OnRoomPlaced -= OnGenerationComplete;
        StartCoroutine(FadeIn());
    }

    // 黒オーバーレイの alpha を 1 → 0 へ補間し、明転しきってからオーバーレイを無効化する。
    private IEnumerator FadeIn()
    {
        if (_darkenOverlay == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < _fadeInDuration)
        {
            elapsed += Time.deltaTime;
            _darkenOverlay.alpha = Mathf.Clamp01(1f - elapsed / _fadeInDuration);
            yield return null;
        }

        _darkenOverlay.alpha = 0f;
        _darkenOverlay.blocksRaycasts = false;
        // 明転後は全画面オーバーレイの描画コストを避けるため無効化する。
        _darkenOverlay.gameObject.SetActive(false);
    }
}
