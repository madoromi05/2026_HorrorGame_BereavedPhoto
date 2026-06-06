using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

public class TestAssressable : MonoBehaviour
{
    public GameObject SpriteObj;

    private SpriteRenderer _spriteRenderer;
    private AsyncOperationHandle<Sprite> _spriteHandle;

    void Start()
    {
        _spriteRenderer = SpriteObj.GetComponent<SpriteRenderer>();

        Addressables.LoadAssetAsync<Sprite>("Assets/Res/Sprites/1.png").Completed += handle =>
        {
            _spriteHandle = handle;
            if(handle.Result == null)
            {
                DebugCustom.LogError("[TestAssressable]Failed to load sprite.");
            }
            _spriteRenderer.sprite = handle.Result;
        };
    }

    private void OnDestroy()
    {
        if(_spriteHandle.IsValid())
        {
            Addressables.Release(_spriteHandle);
        }
    }
}