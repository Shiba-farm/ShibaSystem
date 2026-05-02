using System.Collections.Generic;
using UnityEngine;

// Starter Assets
using StarterAssets;

using Unity.Cinemachine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InventoryMainUI : MonoBehaviour
{
    [Header("Slots")]
    public InventorySlot[] slots;

    [Header("UI Root")]
    public GameObject inventoryPanel;          

    [Header("Freeze Control while open")]
    public Transform player;                  
    public bool unlockCursorOnOpen = true;     
    public Behaviour[] extraDisable;          

    [Header("(Optional)")]
    public bool pauseWithTimeScale = false;

    public static InventoryMainUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    readonly List<Behaviour> _toDisable = new List<Behaviour>();
    bool[] _wasEnabled;
    float _prevTimeScale = 1f;

    void Awake()
    {
        Instance = this;
        // if (inventoryPanel) inventoryPanel.SetActive(false);

        BuildDisableList();            
        _wasEnabled = new bool[_toDisable.Count];
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I)) Toggle();
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    void BuildDisableList()
    {
        _toDisable.Clear();

        if (player != null)
        {
            var tpc = player.GetComponent<PlayerController>();
            if (tpc) _toDisable.Add(tpc);

            var sai = player.GetComponent<StarterAssetsInputs>();
            if (sai) _toDisable.Add(sai);

#if ENABLE_INPUT_SYSTEM
            var pi = player.GetComponent<PlayerInput>();
            if (pi) _toDisable.Add(pi);
#endif
        }
        var cam = Camera.main;
        if (cam)
        {
            var brain = cam.GetComponent<CinemachineBrain>();
            if (brain) _toDisable.Add(brain);

            var cip = cam.GetComponent<CinemachineInputAxisController>();
            if (cip) _toDisable.Add(cip);
        }

        var vcam = FindAnyObjectByType<CinemachineInputAxisController>();
        if (vcam)
        {
            var cip2 = vcam.GetComponent<CinemachineInputAxisController>();
            if (cip2) _toDisable.Add(cip2);
        }

        if (extraDisable != null)
        {
            foreach (var b in extraDisable)
                if (b && !_toDisable.Contains(b))
                    _toDisable.Add(b);
        }
    }

    // ---------------------------------------
    // Toggle / Open / Close
    // ---------------------------------------
    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;

        if (inventoryPanel) inventoryPanel.SetActive(true);

        if (_toDisable.Count == 0) BuildDisableList();
        if (_wasEnabled == null || _wasEnabled.Length != _toDisable.Count)
            _wasEnabled = new bool[_toDisable.Count];

        for (int i = 0; i < _toDisable.Count; i++)
        {
            var b = _toDisable[i];
            if (!b) continue;
            _wasEnabled[i] = b.enabled;
            b.enabled = false;
        }

        if (pauseWithTimeScale)
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        if (unlockCursorOnOpen)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;

        if (inventoryPanel) inventoryPanel.SetActive(false);


        for (int i = 0; i < _toDisable.Count; i++)
        {
            var b = _toDisable[i];
            if (!b) continue;
            bool back = (i < _wasEnabled.Length) ? _wasEnabled[i] : true;
            b.enabled = back;
        }

        if (pauseWithTimeScale)
            Time.timeScale = _prevTimeScale;

        if (unlockCursorOnOpen)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public bool AddItemToInventory(ItemSO item) => AddItemToInventory(item, 1);

    public bool AddItemToInventory(ItemSO item, int amount)
    {
        if (item == null || amount <= 0) return false;

        if (item.isStackable)
        {
            foreach (var slot in slots)
            {
                if (slot.item == item)
                {
                    int max = Mathf.Max(1, item.maxStack);
                    int canAdd = Mathf.Min(amount, max - slot.amount);
                    if (canAdd > 0)
                    {
                        slot.amount += canAdd;
                        slot.UpdateAmountText();
                        amount -= canAdd;
                        if (amount <= 0) return true;
                    }
                }
            }
        }

        foreach (var slot in slots)
        {
            if (slot.item == null)
            {
                int give = item.isStackable ? Mathf.Min(amount, item.maxStack) : 1;
                slot.SetItem(item, give);
                amount -= give;
                if (amount <= 0) return true;
            }
        }

        Debug.Log("Inventory ���!");
        return amount <= 0;
    }
}