using System;
using UnityEngine;

public class HotbarUIController : InventoryMainUIs, ISaveable
{
    // public event Action<ItemSO> OnHeldItemChanged;
    [SerializeField] private HeldItemSignal heldItemSignal;

    private int _selectedIndex = 0;
    private Color _selectedColor = Color.red;


    protected override void OnEnable()
    {
        base.OnEnable();
        InputHandler.Singleton.OnNumkeyTriggered += SelectSlot;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        InputHandler.Singleton.OnNumkeyTriggered -= SelectSlot;
    }

    private void SelectSlot(int index)
    {
        // Debug.Log($"All slot length : {allSlots.Length}");
        int realIndex = index - 1;
        if (realIndex >= allSlots.Length) return;

        // Debug.Log($"Selected index : {_selectedIndex}");

        allSlots[_selectedIndex].SetBackgroundColor(allSlots[_selectedIndex].GetOriginalColor());
        allSlots[realIndex].SetBackgroundColor(_selectedColor);
        _selectedIndex = realIndex;

        // OnHeldItemChanged?.Invoke(allSlots[index].currentItem);
        heldItemSignal.Set(allSlots[realIndex].currentItem);
    }

    public void CaptureState(GameSaveData save, ulong clientId = 0)
    {
        var playerData = save.GetOrCreatePlayer(clientId);
        playerData.heldSlotIndex = _selectedIndex;
    }

    public void RestoreState(GameSaveData save, ulong clientId = 0)
    {
        var playerData = save.FindPlayer(clientId);
        if (playerData == null) return;

        // reuse your existing SelectSlot method — it drives heldItemSignal automatically
        SelectSlot(playerData.heldSlotIndex);
    }
}
