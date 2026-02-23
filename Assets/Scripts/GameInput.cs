using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    private const string PLAYER_PREFS_BINDINGS = "InputBindings";
    public static GameInput Instance { get; private set; }

    public event EventHandler OnInteractAction;//Tạo ra một sự kiện (event) để báo cho các script khác biết khi người chơi nhấn nút “tương tác” (Interact).
    public event EventHandler OnInteractAlternateAction;
    public event EventHandler OnPauseAction;
    public event EventHandler OnBindingRebind;

    

    //------------------------------Skill
    public event EventHandler OnDashAction;
    public event EventHandler OnThrowAction;
    //--camera
    public event EventHandler OnToggleViewAction;

    public enum Binding { 
        Move_Up,
        Move_Down, Move_Left, Move_Right,Interact,
        InteractAlternate,
        Pause,
        Gamepad_Interact,
        Gamepad_InteractAlternate,
        Gamepad_Pause
    }


    private PlayerInputActions playerInputActions;


    private void Awake()
    {
        Instance = this;
        playerInputActions = new PlayerInputActions(); // Khởi tạo lớp auto-gen từ Input System


        if (PlayerPrefs.HasKey(PLAYER_PREFS_BINDINGS))
        {
            playerInputActions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(PLAYER_PREFS_BINDINGS));
        }

        // Gán hàm xử lý khi nhấn nút Interact
        playerInputActions.Player.Interact.performed += Interact_performed;
        playerInputActions.Player.InteractAlternate.performed += InteractAlternate_performed;
        playerInputActions.Player.Pause.performed += Pause_performed;

        // --- THÊM MỚI ---
        playerInputActions.Player.Dash.performed += Dash_performed;
        playerInputActions.Player.Throw.performed += Throw_performed;

        playerInputActions.Player.Enable();

        //--camera
        playerInputActions.Player.ToggleView.performed += ToggleView_performed;

    }

    private void OnDestroy()
    {
        // Đảm bảo bạn có đoạn này (trong code bạn gửi đã có, nhưng hãy check kỹ lại)
        if (playerInputActions != null) // Thêm check null cho chắc
        {
            playerInputActions.Player.Interact.performed -= Interact_performed;
            playerInputActions.Player.InteractAlternate.performed -= InteractAlternate_performed;
            playerInputActions.Player.Pause.performed -= Pause_performed;
            playerInputActions.Player.Dash.performed -= Dash_performed;
            playerInputActions.Player.Throw.performed -= Throw_performed;

            playerInputActions.Player.ToggleView.performed -= ToggleView_performed;

            playerInputActions.Dispose(); // Dòng quan trọng nhất để chống Leak
        }
    }

    private void Pause_performed(InputAction.CallbackContext obj)
    {
        OnPauseAction?.Invoke(this, EventArgs.Empty);
    }

    private void InteractAlternate_performed(InputAction.CallbackContext obj)
    {
        OnInteractAlternateAction?.Invoke(this,EventArgs.Empty);
    }

    private void Interact_performed(InputAction.CallbackContext obj)
    {
        OnInteractAction?.Invoke(this, EventArgs.Empty); // Gửi sự kiện ra ngoài nếu có người “lắng nghe”
    }

    // 4. Hàm xử lý khi bấm nút
    private void ToggleView_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnToggleViewAction?.Invoke(this, EventArgs.Empty);
    }


    public Vector2 GetMovementVectorNormalized()
    {
        Vector2 inputVector = playerInputActions.Player.move.ReadValue<Vector2>();



        inputVector = inputVector.normalized;

        return inputVector;
    }

    public string GetBindingText(Binding binding) 
    {
        switch (binding)
        {
            default:
            case Binding.Move_Up:
                return playerInputActions.Player.move.bindings[1].ToDisplayString();
            case Binding.Move_Down:
                return playerInputActions.Player.move.bindings[2].ToDisplayString();
            case Binding.Move_Left:
                return playerInputActions.Player.move.bindings[3].ToDisplayString();
            case Binding.Move_Right:
                return playerInputActions.Player.move.bindings[4].ToDisplayString();
            case Binding.Interact:
                return playerInputActions.Player.Interact.bindings[0].ToDisplayString();
                    case Binding.InteractAlternate:
                return playerInputActions.Player.InteractAlternate.bindings[0].ToDisplayString();
            case Binding.Pause:
                return playerInputActions.Player.Pause.bindings[0].ToDisplayString();
            case Binding.Gamepad_Interact:
                return playerInputActions.Player.Interact.bindings[1].ToDisplayString();
            case Binding.Gamepad_InteractAlternate:
                return playerInputActions.Player.InteractAlternate.bindings[1].ToDisplayString();
            case Binding.Gamepad_Pause:
                return playerInputActions.Player.Pause.bindings[1].ToDisplayString();
        }

    }

    public void RebindBinding(Binding binding,Action onActionRebound)
    {
        playerInputActions.Player.Disable();
        InputAction inputAction;
        int bindingIndex;
        switch (binding)
        {
            default:
            case Binding.Move_Up:
                inputAction = playerInputActions.Player.move;
                bindingIndex = 1;
                break;
            case Binding.Move_Down:
                inputAction = playerInputActions.Player.move;
                bindingIndex = 2;
                break;
            case Binding.Move_Left:
                inputAction = playerInputActions.Player.move;
                bindingIndex = 3;
                break;
            case Binding.Move_Right:
                inputAction = playerInputActions.Player.move;
                bindingIndex = 4;
                break;
            case Binding.Interact:
                inputAction = playerInputActions.Player.Interact;
                bindingIndex = 0;
                break;
            case Binding.InteractAlternate:
                inputAction = playerInputActions.Player.InteractAlternate;
                bindingIndex = 0;
                break;
            case Binding.Pause:
                inputAction = playerInputActions.Player.Pause;
                bindingIndex = 0;
                break;
            case Binding.Gamepad_Interact:
                inputAction = playerInputActions.Player.Interact;
                bindingIndex = 1;
                break;
            case Binding.Gamepad_InteractAlternate:
                inputAction = playerInputActions.Player.InteractAlternate;
                bindingIndex = 1;
                break;
            case Binding.Gamepad_Pause:
                inputAction = playerInputActions.Player.Pause;
                bindingIndex = 1;
                break;






        }
        inputAction.PerformInteractiveRebinding(bindingIndex)
            .OnComplete(callback =>
            {
                
                callback.Dispose();
                playerInputActions.Player.Enable();
                
                onActionRebound();

                
                PlayerPrefs.SetString(PLAYER_PREFS_BINDINGS, playerInputActions.SaveBindingOverridesAsJson());
                PlayerPrefs.Save();

                OnBindingRebind?.Invoke(this,EventArgs.Empty);
            }) .Start();
        

    }
    // --- CÁC HÀM MỚI ---
    private void Dash_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnDashAction?.Invoke(this, EventArgs.Empty);
    }

    private void Throw_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnThrowAction?.Invoke(this, EventArgs.Empty);
    }
    // -------------------
}
