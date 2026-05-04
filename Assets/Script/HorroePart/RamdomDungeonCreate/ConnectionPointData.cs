using UnityEngine;

namespace DungeonSystem {
    /// <summary>
    /// 部屋の接続口1つのデータ。
    /// PrefabにConnectionPoint_0のような名前の子GameObjectを置き、
    /// そのtransformからDungeonPlacerが生成時に設定する。
    /// </summary>
    public class ConnectionPointData {
        public Vector3 position;
        public Vector3 direction;
        public bool isUsed;
        
        public ConnectionPointData(Vector3 position, Vector3 direction) {
            this.position = position;
            this.direction = direction;
            this.isUsed = false;
        }

        public void MarkUsed() {
            this.isUsed = true;
        }
    }

}
