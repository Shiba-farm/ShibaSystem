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

    /// <summary>True while the left mouse button is physically held. Not gated by InputLocked
    /// so the fishing mini-game hook can respond during critical-panel state.</summary>
    public bool IsLeftClickHeld { get; private set; }

    // --- OBSERVER (Pulse Events) ---
    public event Action OnJumpTriggered;
    public event Action OnInteractTriggered;
    public event Action OnInventoryTriggered;
    public event Action<bool> OnSprintToggled;
    public event Action<int> OnNumkeyTriggered;
    public event Action OnPauseTriggered;
    public event Action OnLeftClick;
    /// <summary>Fires once when the left mouse button (UseTool) is released.</summary>
    public event Action OnLeftClickReleased;
    /// <summary>Fires once on right-click. Used to cancel in-progress tool actions (e.g. fishing).</summary>
    public event Action OnCancelTool;
    public event Action OnOrbitLeftTriggered;
    public event Action OnOrbitRightTriggered;

    /// <summary>
    /// Scroll wheel hotbar — +1 = เลื่อนขึ้น (slot ก่อนหน้า), -1 = เลื่อนลง (slot ถัดไป)
    /// </summary>
    public event Action<int> OnHotbarScrollTriggered;

    private float _prevScrollY = 0f; // ใช้ edge detection แทน cooldown

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
        _controls.Key.Jump.performed += ctx =>
        {
            if (InputLocked) return;
            OnJumpTriggered?.Invoke();
        };

        _controls.Key.Interact.performed += ctx =>
        {
            if (InputLocked) return;
            OnInteractTriggered?.Invoke();
        };

        _controls.Key.Inventory.performed += ctx =>
        {
            OnInventoryTriggered?.Invoke();
        };
        _controls.Key.Pause.performed += ctx =>
        {
            OnPauseTriggered?.Invoke();
        };

        _controls.Key.Sprint.performed += ctx =>
        {
            if (InputLocked) return;
            OnSprintToggled?.Invoke(true);
        };
        _controls.Key.Sprint.canceled += ctx =>
        {
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

        _controls.Key.UseTool.performed += ctx =>
        {
            IsLeftClickHeld = true;
            if (InputLocked) return;
            OnLeftClick?.Invoke();
        };

        // Fire when left mouse button is released — used by fishing hook physics
        _controls.Key.UseTool.canceled += ctx =>
        {
            IsLeftClickHeld = false;
            OnLeftClickReleased?.Invoke();
        };

        _controls.Key.OrbitLeft.performed += ctx =>
        {
            if (InputLocked) return;
            OnOrbitLeftTriggered?.Invoke();
        };

        _controls.Key.OrbitRight.performed += ctx =>
        {
            if (InputLocked) return;
            OnOrbitRightTriggered?.Invoke();
        };
    }

    private void Update()
    {
        if (InputLocked) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        float scrollY = mouse.scroll.ReadValue().y;

        // Edge detection: ยิง event เฉพาะ frame แรกที่ scroll เริ่มขึ้น/ลง
        // ป้องกันการยิงซ้ำในขณะที่ค่า scroll ค้างอยู่หลาย frame
        bool justStartedScrolling = Mathf.Abs(scrollY) > 0.01f && Mathf.Abs(_prevScrollY) < 0.01f;
        if (justStartedScrolling)
        {
            int direction = scrollY > 0f ? -1 : 1; // scroll up = slot ก่อนหน้า, scroll down = slot ถัดไป
            OnHotbarScrollTriggered?.Invoke(direction);
        }

        _prevScrollY = scrollY;

        // Right-click → cancel current tool action (e.g. abort fishing while waiting for bite)
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            OnCancelTool?.Invoke();
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
