using UnityEngine;
using System.Collections.Generic;

namespace DungeonSystem
{
    /// <summary>
    /// ダンジョン生成パラメータをInspectorから編集可能にするScriptableObject。
    /// マジックナンバーをコードに直書きしないためにここで一元管理する。
    /// </summary>
    [CreateAssetMenu(fileName = "DungeonConfig", menuName = "DungeonSystem/DungeonConfig")]
    public class DungeonConfig : ScriptableObject
    {
        [Header("グリッド設定")]
        [SerializeField] private int _gridColumns = 5;
        [SerializeField] private int _gridRows = 5;

        [Header("部屋設定")]
        [SerializeField] private List<RoomEntry> _roomEntries = new List<RoomEntry>();

        [Header("通路設定")]
        [SerializeField] private GameObject _corridorPrefab;

        /// <summary>通路プレハブのZ方向の基準長（メートル）。scale.z計算に使用する</summary>
        [SerializeField] private float _corridorBaseLength = 1f;

        /// <summary>通路の幅（メートル）。貫通チェックのBounds計算に使用する</summary>
        [SerializeField] private float _corridorWidth = 2f;

        [Header("ループ設定")]
        [Range(0, 5)]
        [SerializeField] private int _extraLoopCount = 1;

        [Header("配置設定")]
        /// <summary>重なり時の部屋配置リトライ上限</summary>
        [SerializeField] private int _maxRetryCount = 30;

        /// <summary>
        /// セルサイズ。未設定(0)の場合は最大部屋サイズ + 最小通路長から自動計算する。
        /// 明示的に指定したい場合のみ0以外を設定する。
        /// </summary>
        [SerializeField] private float _cellSizeOverride = 0f;

        /// <summary>最小通路長（メートル）。cellSize自動計算に使用する</summary>
        [SerializeField] private float _minCorridorLength = 4f;

        // --- プロパティ ---

        public int GridColumns => _gridColumns;
        public int GridRows => _gridRows;

        public IReadOnlyList<RoomEntry> RoomEntries => _roomEntries;

        public GameObject CorridorPrefab => _corridorPrefab;
        public float CorridorBaseLength => _corridorBaseLength;
        public float CorridorWidth => _corridorWidth;

        public int ExtraLoopCount => _extraLoopCount;
        public int MaxRetryCount => _maxRetryCount;

        /// <summary>
        /// セルサイズを返す。
        /// _cellSizeOverrideが0の場合は最大部屋サイズ + 最小通路長から自動計算する。
        /// </summary>
        public float GetCellSize()
        {
            if (_cellSizeOverride > 0f)
                return _cellSizeOverride;

            float maxRoomSize = 0f;
            foreach (var entry in _roomEntries)
            {
                float size = Mathf.Max(entry.roomSize.x, entry.roomSize.y);
                if (size > maxRoomSize)
                    maxRoomSize = size;
            }

            return maxRoomSize + _minCorridorLength;
        }
    }
}