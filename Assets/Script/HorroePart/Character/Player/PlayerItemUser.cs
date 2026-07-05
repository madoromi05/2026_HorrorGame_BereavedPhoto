using HorrorGame.Item;
using UnityEngine;

/// <summary>
/// 妨害アイテム使用処理。UseItem入力でカメラ前方にアイテムを投げ、
/// 当たった敵を一定時間停止させる。
/// Playerプレハブにアタッチして使用する。
/// </summary>
public class PlayerItemUser : MonoBehaviour
{
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private float      _throwSpeed    = 15f;
    [SerializeField] private float      _stunDuration  = 5f;

    private Inventory             _inventory;
    private InputPlayerController _input;

    private void Awake()
    {
        _inventory = GetComponent<Inventory>();
        _input     = GetComponent<InputPlayerController>();
    }

    private void OnEnable()
    {
        if (_input != null)
            _input.OnUseItemPerformed += OnUseItem;
    }

    private void OnDisable()
    {
        if (_input != null)
            _input.OnUseItemPerformed -= OnUseItem;
    }

    private void OnUseItem()
    {
        if (_projectilePrefab == null)
        {
            DebugCustom.LogWarning("[PlayerItemUser] _projectilePrefab が未設定です。");
            return;
        }

        if (!_inventory.ConsumeItem(ItemType.ObstructionItem)) return;

        var cam       = Camera.main;
        var spawnPos  = cam.transform.position + cam.transform.forward * 0.5f;
        var go        = Instantiate(_projectilePrefab, spawnPos, cam.transform.rotation);

        var projectileCol = go.GetComponent<Collider>();
        if (projectileCol != null)
        {
            foreach (var col in GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(projectileCol, col);
        }

        var projectile = go.GetComponent<ObstructionItemProjectile>();
        if (projectile != null)
            projectile.Init(cam.transform.forward * _throwSpeed, _stunDuration);
    }
}
