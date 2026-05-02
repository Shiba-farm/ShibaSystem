using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public static InputHandler Singleton { get; private set; }
    public bool InputLocked { get; set; }

    private PlayerControls _controls;

    // --- POLLING (Continuous Values) ---
    public Vector2 MoveInput => InputLocked ? Vector2.zero : _controls.Key.Move.ReadValue<Vector2>();
    public Vector2 MousePosition => _controls.Key.Pointer.ReadValue<Vector2>();

    public bool IsSprinting => !InputLocked && _controls.Key.Sprint.IsPressed();

    // --- OBSERVER (Pulse Events) ---
    public event Action OnJumpTriggered;
    public event Action OnInteractTriggered;
    public event Action OnInventoryTriggered;
    public event Action<bool> OnSprintToggled;
    public event Action<int> OnNumkeyTriggered;
    public event Action OnPauseTriggered;

    private void Awake()
    {
        if (Singleton == null)
        {
            Singleton = this;
            _controls = new PlayerControls();

            BindActions();

            _controls.Enable();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void BindActions()
    {
        _controls.Key.Jump.performed += ctx => {
            if (InputLocked) return;
            OnJumpTriggered?.Invoke();
        };

        _controls.Key.Interact.performed += ctx => {
            if (InputLocked) return;
            OnInteractTriggered?.Invoke();
        };

        _controls.Key.Inventory.performed += ctx => {
            OnInventoryTriggered?.Invoke();
        };
        _controls.Key.Pause.performed += ctx => {
            OnPauseTriggered?.Invoke();
        };

        _controls.Key.Sprint.performed += ctx => {
            if (InputLocked) return;
            OnSprintToggled?.Invoke(true);
        };
        _controls.Key.Sprint.canceled += ctx => {
            OnSprintToggled?.Invoke(false);
        };

        _controls.Key.Numkey.performed += ctx =>
        {
            if (InputLocked) return;
            if (int.TryParse(ctx.control.name, out int val))
            {
                OnNumkeyTriggered?.Invoke(val);
            }
        };
    }

    private void OnDestroy()
    {
        if (_controls != null)
        {
            _controls.Disable();
            _controls.Dispose();
        }
    }
}
