using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputTitleController : MonoBehaviour, InputSystem_Actions.ITitleActions
{
    private InputSystem_Actions _inputActions;
    public event Action OnMenuPerformed;

    //---------- 有効化 ----------
    private void OnEnable()
    {
        if (_inputActions == null)
        {
            _inputActions = new InputSystem_Actions();
            _inputActions.Title.SetCallbacks(this);
        }
        _inputActions.Title.Enable();
    }
    //---------- 無効化 ----------
    private void OnDisable()
    {
        _inputActions?.Title.Disable();
    }
    //---------- コールバック実装 ----------
    public void OnMenu(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnMenuPerformed?.Invoke();
    }
}