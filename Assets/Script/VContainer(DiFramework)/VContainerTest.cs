using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using UnityEngine.InputSystem;

public class TestVContainer : MonoBehaviour
{
    [Inject] private OnePlaySaveData _onePlaySaveData;
    [Inject] private IDebugLogger _logger;

    private void Update()
    {
        _onePlaySaveData.Score++;
        _logger.Log($"Score: {_onePlaySaveData.Score}");

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            _logger.Log("input R");
            SceneManager.LoadScene(gameObject.scene.name);
        }
    }
}