using UnityEngine;
using System.Collections.Generic;

namespace DungeonSystem
{
    /// <summary>
    /// 通路1つの経路データ。
    /// waypointsの先頭がstartPoint、末尾がendPointの位置になる。
    /// 直線の場合は2点、L字の場合は3点になる。
    /// </summary>
    public class CorridorData
    {
        public ConnectionPointData StartPoint { get; }
        public ConnectionPointData EndPoint { get; }

        /// <summary>
        /// 通路の経由点リスト。
        /// [0]=始点, [n-1]=終点, L字の場合[1]が折れ曲がり点。
        /// </summary>
        public IReadOnlyList<Vector3> Waypoints { get; }

        public CorridorData(
            ConnectionPointData startPoint,
            ConnectionPointData endPoint,
            List<Vector3> waypoints)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
            Waypoints = waypoints;
        }
    }
}