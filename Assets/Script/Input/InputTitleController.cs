using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputTitleController : MonoBehaviour, InputSystem_Actions.ITitleActions
{
    private InputSystem_Actions _inputActions;
    public event Action OnMenuPerformed;
    public event Action OnMenuHeld;      // 押した瞬間（context.started）
    public event Action OnMenuReleased;  // 離した瞬間（context.canceled）

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
    //---------- コールバック処理 ----------
    public void OnMenu(InputAction.CallbackContext context)
    {
        if (context.started)   OnMenuHeld?.Invoke();
        if (context.performed) OnMenuPerformed?.Invoke();
        if (context.canceled)  OnMenuReleased?.Invoke();
    }
}
