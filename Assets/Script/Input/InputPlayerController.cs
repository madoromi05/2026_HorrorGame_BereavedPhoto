using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputPlayerController : MonoBehaviour, InputSystem_Actions.IPlayerActions
{
    public event Action<Vector2> OnMovePerformed;
    public event Action<Vector2> OnLookPerformed;
    public event Action<bool> OnCameraPerformed;
    public event Action<bool> OnSprintPerformed;
    public event Action OnInteractPerformed;
    public event Action OnHandLightPerformed;
    public event Action OnInteractHeld;
    public event Action OnInteractReleased;
    public event Action<bool> OnLeanLeftPerformed;
    public event Action<bool> OnLeanRightPerformed;
    public event Action<bool> OnHoldBreathPerformed;
    public event Action OnMenuPerformed;
    public event Action OnUseItemPerformed;

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

        // 無効化した瞬間にラッチ済みの移動入力を打ち消す。
        // これをしないと直前の移動値が PlayerMover に残り、UI表示中も滑り続ける。
        if (!enabled)
            OnMovePerformed?.Invoke(Vector2.zero);
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

    // しゃがみ機能は削除済み。IPlayerActions のインターフェース実装のため空メソッドとして残す。
    public void OnCrouch(InputAction.CallbackContext context)
    {
    }

    public void OnSprint(InputAction.CallbackContext context)
    {

        if (!_isPlayerInputEnabled) return;
        if (context.performed)
            OnSprintPerformed?.Invoke(true);
        else if (context.canceled)
            OnSprintPerformed?.Invoke(false);
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

    public void OnLeanLeft(InputAction.CallbackContext context)
    {
        if (!_isPlayerInputEnabled) return;
        if (context.performed)
            OnLeanLeftPerformed?.Invoke(true);
        else if (context.canceled)
            OnLeanLeftPerformed?.Invoke(false);
    }

    public void OnLeanRight(InputAction.CallbackContext context)
    {
        if (!_isPlayerInputEnabled) return;
        if (context.performed)
            OnLeanRightPerformed?.Invoke(true);
        else if (context.canceled)
            OnLeanRightPerformed?.Invoke(false);
    }

    public void OnHoldBreath(InputAction.CallbackContext context)
    {
        if (!_isPlayerInputEnabled) return;
        if (context.performed)
            OnHoldBreathPerformed?.Invoke(true);
        else if (context.canceled)
            OnHoldBreathPerformed?.Invoke(false);
    }

    void InputSystem_Actions.IPlayerActions.OnMenu(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnMenuPerformed?.Invoke();
    }

    public void OnUseItem(InputAction.CallbackContext context)
    {
        if (!_isPlayerInputEnabled) return;
        if (context.performed)
            OnUseItemPerformed?.Invoke();
    }
}
