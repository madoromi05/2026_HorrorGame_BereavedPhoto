using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputPlayerController : MonoBehaviourÅ@, InputSystem_Actions.IPlayerActions
{
    private InputSystem_Actions _inputActions;

    public event Action<Vector2> OnMovePerformed;
    public event Action<Vector2> OnLookPerformed;
    public event Action OnInteractPerformed;

    //---------- óLå¯âª ----------
    private void OnEnable()
    {
        if (_inputActions == null)
        {
            _inputActions = new InputSystem_Actions();
            _inputActions.Player.SetCallbacks(this);
        }
        _inputActions.Player.Enable();
    }

    //---------- ñ≥å¯âª ----------
    private void OnDisable()
    {
        _inputActions?.Player.Disable();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled)
        {
            OnMovePerformed?.Invoke(context.ReadValue<Vector2>());
        }
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled)
        {
            OnLookPerformed?.Invoke(context.ReadValue<Vector2>());
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnInteractPerformed?.Invoke();
        }
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        throw new NotImplementedException();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        throw new NotImplementedException();
    }
}