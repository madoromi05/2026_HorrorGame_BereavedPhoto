using UnityEngine;

public enum GhostType { Mother, Father }

/// <summary>幽霊の種類を識別するコンポーネント。敵プレハブにアタッチして使用する。</summary>
public class GhostIdentity : MonoBehaviour
{
    [SerializeField] private GhostType _ghostType;

    public GhostType GhostType => _ghostType;
}
