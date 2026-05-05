using UnityEngine;
using System.Collections.Generic;

namespace DungeonSystem
{
    /// <summary>
    /// 部屋リストからPrim法で最小全域木を構築し、
    /// ループ辺を追加してグラフ（接続ペアリスト）を返すクラス。
    /// 全部屋の連結を保証する。
    /// </summary>
    public class GraphBuilder
    {
        /// 部屋IDのペアで接続関係を表す
        public struct Edge
        {
            public int RoomIdA;
            public int RoomIdB;
        }

        /// <summary>
        /// 最小全域木 + ループ辺を構築して返す。
        /// </summary>
        public List<Edge> Build(
            IReadOnlyList<RoomData> rooms,
            int extraLoopCount,
            System.Random random)
        {
            if (rooms.Count < 2)
                return new List<Edge>();

            var mstEdges = BuildMst(rooms);
            var loopEdges = AddLoopEdges(mstEdges, rooms, extraLoopCount, random);

            var result = new List<Edge>(mstEdges);
            result.AddRange(loopEdges);
            return result;
        }

        /// <summary>
        /// Prim法で最小全域木を構築する。
        /// 距離にはXZ平面のマンハッタン距離を使用する。
        /// </summary>
        private List<Edge> BuildMst(IReadOnlyList<RoomData> rooms)
        {
            var mst = new List<Edge>();
            var inMst = new HashSet<int>();

            inMst.Add(rooms[0].Id);

            while (inMst.Count < rooms.Count)
            {
                float minDist = float.MaxValue;
                int fromId = -1;
                int toId = -1;

                foreach (var room in rooms)
                {
                    if (!inMst.Contains(room.Id)) continue;

                    foreach (var candidate in rooms)
                    {
                        if (inMst.Contains(candidate.Id)) continue;

                        float dist = Vector3.Distance(room.Position, candidate.Position);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            fromId = room.Id;
                            toId = candidate.Id;
                        }
                    }
                }

                if (toId < 0) break;

                inMst.Add(toId);
                mst.Add(new Edge { RoomIdA = fromId, RoomIdB = toId });
            }

            return mst;
        }

        /// <summary>
        /// MSTに含まれない辺からループ辺を追加する。
        /// 条件①: 隣接部屋同士のみ候補にする（長距離ループを避ける）
        /// 条件②: Start-Boss直結は除外する（難易度設計を守る）
        /// 条件③: 両端がすでに2本以上接続済みなら除外する（通路集中を防ぐ）
        /// </summary>
        private List<Edge> AddLoopEdges(
            List<Edge> mst,
            IReadOnlyList<RoomData> rooms,
            int extraLoopCount,
            System.Random random)
        {
            // MST辺をハッシュセット化して高速検索する
            var mstSet = new HashSet<(int, int)>();
            foreach (var e in mst)
                mstSet.Add(MakeKey(e.RoomIdA, e.RoomIdB));

            // MSTでの接続数をカウントする
            var connectionCount = new Dictionary<int, int>();
            foreach (var room in rooms)
                connectionCount[room.Id] = 0;
            foreach (var e in mst)
            {
                connectionCount[e.RoomIdA]++;
                connectionCount[e.RoomIdB]++;
            }

            // StartとBossのIDを特定する
            int startId = -1, bossId = -1;
            foreach (var room in rooms)
            {
                if (room.RoomType == RoomType.Start) startId = room.Id;
            }

            // 隣接判定の閾値: cellSizeの1.5倍以内を隣接とみなす
            // 部屋間の距離はcellSize単位なので斜め隣は √2 * cellSize になる
            // 直交隣接のみにしたいので距離を部屋間のおよそ1.1倍で閾値を設ける
            float adjacencyThreshold = GetAdjacencyThreshold(rooms);

            var candidates = new List<Edge>();
            for (int i = 0; i < rooms.Count; ++i)
            {
                for (int j = i + 1; j < rooms.Count; ++j)
                {
                    int idA = rooms[i].Id;
                    int idB = rooms[j].Id;

                    // 条件①: 隣接部屋同士のみ
                    float dist = Vector3.Distance(
                        rooms[i].Position, rooms[j].Position);
                    if (dist > adjacencyThreshold) continue;

                    // MST既存辺は除外する
                    if (mstSet.Contains(MakeKey(idA, idB))) continue;

                    // 条件②: Start-Boss直結を除外する
                    bool isStartBoss =
                        (idA == startId && idB == bossId) ||
                        (idA == bossId && idB == startId);
                    if (isStartBoss) continue;

                    // 条件③: 両端ともに2本以上接続済みなら除外する
                    if (connectionCount[idA] >= 2
                     && connectionCount[idB] >= 2) continue;

                    candidates.Add(new Edge { RoomIdA = idA, RoomIdB = idB });
                }
            }

            // シャッフルしてextraLoopCount本取る
            Shuffle(candidates, random);

            var result = new List<Edge>();
            int addCount = Mathf.Min(extraLoopCount, candidates.Count);
            for (int i = 0; i < addCount; ++i)
                result.Add(candidates[i]);

            return result;
        }

        /// <summary>
        /// 直交隣接の閾値を部屋リストの最小距離 × 1.1 から求める。
        /// 部屋数が2未満の場合はfloat.MaxValueを返す。
        /// </summary>
        private float GetAdjacencyThreshold(IReadOnlyList<RoomData> rooms)
        {
            if (rooms.Count < 2) return float.MaxValue;

            float minDist = float.MaxValue;
            for (int i = 0; i < rooms.Count; ++i)
                for (int j = i + 1; j < rooms.Count; ++j)
                {
                    float d = Vector3.Distance(rooms[i].Position, rooms[j].Position);
                    if (d < minDist) minDist = d;
                }

            return minDist * 1.1f;
        }

        private (int, int) MakeKey(int a, int b)
            => a < b ? (a, b) : (b, a);

        private void Shuffle<T>(List<T> list, System.Random random)
        {
            for (int i = list.Count - 1; i > 0; --i)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}