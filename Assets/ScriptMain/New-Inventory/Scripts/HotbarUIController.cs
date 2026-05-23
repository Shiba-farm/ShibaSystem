using System;
using UnityEngine;

public class HotbarUIController : InventoryMainUIs, ILocalSaveable
{
    // public event Action<ItemSO> OnHeldItemChanged;
    [SerializeField] private HeldItemSignal heldItemSignal;

    private int _selectedIndex = 0;
    private int _lastnumEnter = 1;
    private Color _selectedColor = Color.red;
    private ulong _ownerClientId;
    public ulong OwnerClientId => _ownerClientId;

    public bool IsPlayerSaveable => true;

    public void SetOwnerClientId(ulong clientId)
    {
        _ownerClientId = clientId;
        Debug.Log("Set hotbar");
        TryRegister();
    }

    private void TryRegister()
    {
        if (SaveLoadManager.Instance != null)
            SaveLoadManager.Instance.Register(this);
    }


    protected override void OnEnable()
    {
        base.OnEnable();
        InputHandler.Singleton.OnNumkeyTriggered += SelectSlot;
        TryRegister();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        InputHandler.Singleton.OnNumkeyTriggered -= SelectSlot;
        SaveLoadManager.Instance?.Unregister(this);
    }

    // private void SelectSlot(int index)
    // {
    //     // Debug.Log($"All slot length : {allSlots.Length}");
    //     int realIndex = index - 1;
    //     if (realIndex >= allSlots.Length) return;

    //     // Debug.Log($"Selected index : {_selectedIndex}");

    //     allSlots[_selectedIndex].SetBackgroundColor(allSlots[_selectedIndex].GetOriginalColor());
    //     allSlots[realIndex].SetBackgroundColor(_selectedColor);
    //     _selectedIndex = realIndex;
    //     _lastnumEnter = index;

    //     // OnHeldItemChanged?.Invoke(allSlots[index].currentItem);
    //     heldItemSignal.Set(allSlots[realIndex].currentItem, realIndex);
    // }

    private void SelectSlotByRealIndex(int realIndex)
    {
        if (realIndex < 0 || realIndex >= allSlots.Length) return;

        allSlots[_selectedIndex].SetBackgroundColor(allSlots[_selectedIndex].GetOriginalColor());
        allSlots[realIndex].SetBackgroundColor(_selectedColor);
        _selectedIndex = realIndex;

        heldItemSignal.Set(allSlots[realIndex].currentItem, realIndex);
    }

    // Input-driven — stays 1-based
    private void SelectSlot(int index)
    {
        int realIndex = index - 1;
        if (realIndex >= allSlots.Length) return;
        _lastnumEnter = index;
        SelectSlotByRealIndex(realIndex);
    }

    public void CaptureState(GameSaveData save, ulong clientId = 0)
    {
        var playerData = save.GetOrCreatePlayer(clientId);
        playerData.heldSlotIndex = _lastnumEnter;

        Debug.Log("Save player held item Game State");
    }

    public void RestoreState(GameSaveData save, ulong clientId = 0)
    {
        var playerData = save.FindPlayer(clientId);
        if (playerData == null) return;

        // reuse your existing SelectSlot method — it drives heldItemSignal automatically
        SelectSlotByRealIndex(playerData.heldSlotIndex);
    }
}
