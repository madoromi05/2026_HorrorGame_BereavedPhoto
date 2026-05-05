using UnityEngine;

namespace DungeonSystem
{
    /// <summary>
    /// ダンジョン生成の全体フローを制御するクラス。
    /// 各クラスの呼び出し順序の管理のみを担当し、ロジックは持たない。
    /// </summary>
    public class DungeonDirector : MonoBehaviour
    {
        [SerializeField] private DungeonConfig _config;

        /// <summary>
        /// 生成したGameObjectをまとめる親Transform。
        /// 未設定の場合はこのGameObjectを親にする。
        /// </summary>
        [SerializeField] private Transform _dungeonRoot;

        [SerializeField] private DungeonPlacer _placer;

        /// <summary>
        /// 0以下の場合はTime.timeをシードに使用する。
        /// デバッグ時は固定値を設定することで再現可能な生成ができる。
        /// </summary>
        [SerializeField] private int _randomSeed = 0;

        private void Start()
        {
            Generate();
        }

        /// <summary>
        /// ダンジョンを生成する。
        /// Inspectorのボタンや外部スクリプトからも呼び出し可能にしている。
        /// </summary>
        public void Generate()
        {
            if (_config == null)
            {
                Debug.LogError("DungeonConfigが設定されていません");
                return;
            }

            if (_placer == null)
            {
                Debug.LogError("DungeonPlacerが設定されていません");
                return;
            }

            var root = _dungeonRoot != null ? _dungeonRoot : transform;
            var random = _randomSeed > 0
                ? new System.Random(_randomSeed)
                : new System.Random((int)Time.time);

            // データ構造を初期化する
            var data = new DungeonData();

            // 部屋を配置する（DungeonDataにRoomDataリストを構築）
            var overlapChecker = new OverlapChecker();
            var roomTypeRule = new RoomTypeRule();
            var roomPlacer = new RoomPlacer(overlapChecker, roomTypeRule);
            roomPlacer.Place(data, _config, random);

            // 部屋をInstantiateしてConnectionPointDataを設定する
            _placer.PlaceRooms(data, _config, root);

            // 最小全域木 + ループ辺でグラフを構築する
            var graphBuilder = new GraphBuilder();
            var edges = graphBuilder.Build(
                data.Rooms, _config.ExtraLoopCount, random);

            // 各辺の通路経路を計算する
            var corridorBuilder = new CorridorBuilder(overlapChecker);
            corridorBuilder.Build(data, edges, _config);

            // 通路をInstantiateする
            _placer.PlaceCorridors(data, _config, root);

            Debug.Log(
                $"ダンジョン生成完了: 部屋数={data.Rooms.Count} 通路数={data.Corridors.Count}");
        }

#if UNITY_EDITOR
        /// <summary>
        /// Gizmosで部屋のBoundsと通路の経路を可視化する。
        /// Sceneビューでデバッグしやすくするためのエディタ専用処理。
        /// </summary>
        private void OnDrawGizmos()
        {
            if (_config == null) return;

            float cellSize = _config.GetCellSize();
            int columns = _config.GridColumns;
            int rows = _config.GridRows;

            // グリッドラインを描画する
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            for (int c = 0; c <= columns; ++c)
                Gizmos.DrawLine(
                    new Vector3(c * cellSize, 0f, 0f),
                    new Vector3(c * cellSize, 0f, rows * cellSize));
            for (int r = 0; r <= rows; ++r)
                Gizmos.DrawLine(
                    new Vector3(0f, 0f, r * cellSize),
                    new Vector3(columns * cellSize, 0f, r * cellSize));
        }
#endif
    }
}