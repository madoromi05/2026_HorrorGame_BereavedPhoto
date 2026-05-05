using UnityEngine;
using System.Collections.Generic;

namespace DungeonSystem
{
    /// <summary>
    /// DungeonDataをもとにPrefabをInstantiateするクラス。
    /// データの変更は行わず、GameObject生成のみを担当する。
    /// </summary>
    public class DungeonPlacer : MonoBehaviour
    {
        private const string kConnectionPointPrefix = "ConnectionPoint_";

        /// <summary>
        /// 部屋をInstantiateし、ConnectionPointDataをRoomDataに設定する。
        /// Instantiate後に接続ポイントのワールド座標が確定するため、
        /// このタイミングでConnectionPointDataを構築する。
        /// </summary>
        public void PlaceRooms(
            DungeonData data,
            DungeonConfig config,
            Transform parent)
        {
            foreach (var room in data.Rooms)
            {
                var entry = FindEntry(config.RoomEntries, room.RoomType);
                if (entry?.prefab == null)
                {
                    Debug.LogWarning(
                        $"RoomType {room.RoomType} のPrefabが設定されていません");
                    continue;
                }

                var instance = Instantiate(
                    entry.prefab,
                    room.Position,
                    Quaternion.identity,
                    parent);
                instance.name = $"Room_{room.Id}_{room.RoomType}";

                // Instantiate後にConnectionPointをワールド座標で収集する
                SetupConnectionPoints(room, instance);
            }
        }

        /// <summary>
        /// 通路をInstantiateする。
        /// ウェイポイント間の各セグメントをcorridorPrefabをスケーリングして配置する。
        /// </summary>
        public void PlaceCorridors(
            DungeonData data,
            DungeonConfig config,
            Transform parent)
        {
            if (config.CorridorPrefab == null)
            {
                Debug.LogWarning("CorridorPrefabが設定されていません");
                return;
            }

            int corridorIndex = 0;
            foreach (var corridor in data.Corridors)
            {
                for (int i = 0; i + 1 < corridor.Waypoints.Count; ++i)
                {
                    var start = corridor.Waypoints[i];
                    var end = corridor.Waypoints[i + 1];

                    // 始点と終点が同じ場合はセグメントをスキップする
                    if (Vector3.Distance(start, end) < 0.001f) continue;

                    PlaceCorridorSegment(
                        start, end,
                        config.CorridorPrefab,
                        config.CorridorBaseLength,
                        parent,
                        $"Corridor_{corridorIndex}_Seg{i}");
                }
                corridorIndex++;
            }
        }

        /// <summary>
        /// セグメント1本分の通路Prefabを配置する。
        /// Prefabのforward(Z+)方向が通路の進行方向になる前提でRotationを設定する。
        /// scale.zで通路の長さを調整する。
        /// </summary>
        private void PlaceCorridorSegment(
            Vector3 start,
            Vector3 end,
            GameObject prefab,
            float baseLength,
            Transform parent,
            string instanceName)
        {
            var direction = end - start;
            var length = direction.magnitude;

            var center = (start + end) * 0.5f;
            var rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

            var instance = Instantiate(prefab, center, rotation, parent);
            instance.name = instanceName;

            // Z方向のスケールで長さを調整する
            // baseLengthが0の場合のゼロ除算を防ぐ
            if (baseLength > 0.001f)
            {
                var scale = instance.transform.localScale;
                scale.z = length / baseLength;
                instance.transform.localScale = scale;
            }
        }

        /// <summary>
        /// Instantiate済みのGameObjectからConnectionPoint_*を収集し、
        /// RoomDataのConnectionPointsリストに設定する。
        /// </summary>
        private void SetupConnectionPoints(RoomData room, GameObject instance)
        {
            // RoomDataのConnectionPointsはreadonlyなため
            // 内部リストにキャストして設定する
            var points = room.ConnectionPoints as List<ConnectionPointData>;
            if (points == null) return;

            int index = 0;
            while (true)
            {
                var pointName = kConnectionPointPrefix + index;
                var pointTransform = instance.transform.Find(pointName);
                if (pointTransform == null) break;

                points.Add(new ConnectionPointData(
                    pointTransform.position,
                    pointTransform.forward));

                index++;
            }

            if (points.Count == 0)
                Debug.LogWarning(
                    $"{instance.name} に {kConnectionPointPrefix}0 が見つかりません");
        }

        private RoomEntry FindEntry(
            IReadOnlyList<RoomEntry> entries, RoomType type)
        {
            foreach (var entry in entries)
                if (entry.roomType == type) return entry;
            return null;
        }
    }
}