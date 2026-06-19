using UnityEngine;

/// <summary>
/// 妨害アイテムの投擲物。敵に当たったらStunし、自身は破壊される。
/// RigidbodyとColliderを持つプレハブにアタッチして使用する。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ObstructionItemProjectile : MonoBehaviour
{
    private float _stunDuration;

    public void Init(Vector3 velocity, float stunDuration)
    {
        _stunDuration = stunDuration;
        GetComponent<Rigidbody>().linearVelocity = velocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        var enemy = collision.gameObject.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            enemy.Stun(_stunDuration);
            DebugCustom.Log($"[ObstructionItemProjectile] {enemy.name} を{_stunDuration}秒停止");
        }
        Destroy(gameObject);
    }
}
