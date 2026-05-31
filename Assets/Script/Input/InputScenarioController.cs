using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputScenarioController : MonoBehaviour, InputSystem_Actions.IScenerioActions
{
    private InputSystem_Actions _inputActions;

    public event Action OnClickPerformed;

    //---------- 有効化 ----------
    private void OnEnable()
    {
        if (_inputActions == null)
        {
            _inputActions = new InputSystem_Actions();
            _inputActions.Scenerio.SetCallbacks(this);
        }
        _inputActions.Scenerio.Enable();
    }

    //---------- 無効化 ----------
    private void OnDisable()
    {
        _inputActions?.Scenerio.Disable();
    }

    //---------- コールバック実装 ----------
    public void OnCancel(InputAction.CallbackContext context)
    {
        // Scenario パートでは Cancel を扱わない（未使用）
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnClickPerformed?.Invoke();
        }
    }

    public void OnMiddleClick(InputAction.CallbackContext context)
    {
        // throw new System.NotImplementedException();
    }

    public void OnPoint(InputAction.CallbackContext context)
    {
        // throw new System.NotImplementedException();
    }

    public void OnRightClick(InputAction.CallbackContext context)
    {
        //  throw new System.NotImplementedException();
    }

    public void OnScrollWheel(InputAction.CallbackContext context)
    {
        // throw new System.NotImplementedException();
    }
}
