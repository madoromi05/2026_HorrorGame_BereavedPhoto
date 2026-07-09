using UnityEngine;

/// <summary>幽霊の種類を識別するコンポーネント。敵プレハブにアタッチして使用する。</summary>
public class GhostIdentity : MonoBehaviour
{
    [SerializeField] private EnemyType _ghostType;

    public EnemyType GhostType => _ghostType;
}
