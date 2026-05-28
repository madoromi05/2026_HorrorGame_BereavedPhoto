using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputPlayerController : MonoBehaviour　, InputSystem_Actions.IPlayerActions
{
    public event Action<Vector2> OnMovePerformed;
    public event Action<Vector2> OnLookPerformed;
    public event Action<bool> OnCameraPerformed;
    public event Action<bool> OnCrouchPerformed;
    public event Action OnInteractPerformed;
    public event Action OnHandLightPerformed;
    public event Action OnInteractHeld;
    public event Action OnInteractReleased;

    private bool _isPlayerInputEnabled = true;
    private InputSystem_Actions _inputActions;

    //---------- 有効化 ----------
    private void OnEnable()
    {
        if (_inputActions == null)
        {
            _inputActions = new InputSystem_Actions();
            _inputActions.Player.SetCallbacks(this);
        }
        _inputActions.Player.Enable();
    }

    //---------- 無効化 ----------
    private void OnDisable()
    {
        _inputActions?.Player.Disable();
    }

    /// <summary>
    /// メモ表示など、プレイヤー操作を封じる必要があるときに呼ぶ。
    /// Move・Look・Interactのイベントを止める。
    /// </summary>
    public void SetPlayerInputEnabled(bool enabled)
    {
        _isPlayerInputEnabled = enabled;
    }

    //-------------------- コールバック群 --------------------
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!_isPlayerInputEnabled) return;
        if (context.performed || context.canceled)
            OnMovePerformed?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (!_isPlayerInputEnabled) return;
        if (context.performed || context.canceled)
            OnLookPerformed?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (_isPlayerInputEnabled)
                OnInteractPerformed?.Invoke();
            else
                OnInteractHeld?.Invoke();
        }

        if (context.canceled)
            OnInteractReleased?.Invoke();
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (!_isPlayerInputEnabled) return;
        if (context.performed)
        {
            Debug.Log("Crouch performed, invoking event with true...");
            OnCrouchPerformed?.Invoke(true);
        }
        else if (context.canceled)
        {
            Debug.Log("Crouch canceled, invoking event with false...");
            OnCrouchPerformed?.Invoke(false);
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        throw new NotImplementedException();
    }

    public void OnCamera(InputAction.CallbackContext context)
    {
        if (!_isPlayerInputEnabled) return;
        if (context.performed)
            OnCameraPerformed?.Invoke(true);
        else if (context.canceled)
            OnCameraPerformed?.Invoke(false);
    }

    public void OnHandLight(InputAction.CallbackContext context)
    {
        if (!_isPlayerInputEnabled) return;
        if (context.performed)
        {
            OnHandLightPerformed?.Invoke();
        }
    }
}
