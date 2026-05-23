using UnityEngine;

public class GlobalSaveContext : MonoBehaviour
{
    public static GlobalSaveContext Instance { get; private set; }

    public bool ShouldLoadOnStart { get; private set; } = false;
    public int TargetSlot { get; private set; } = 0;   // which slot to load
    public string TargetScene { get; private set; } = "MainGame";
    private const int MaxSlots = 5;
    public string PendingWorldName { get; private set; } = "";

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RequestLoad(int slotIndex, string sceneName)
    {
        ShouldLoadOnStart = true;
        TargetSlot = slotIndex;
        TargetScene = sceneName;
    }

    public void RequestNewGame(int slotIndex, string worldName)
    {
        ShouldLoadOnStart = false;
        TargetSlot = slotIndex;   // still need to know which slot to SAVE into later
        TargetScene = "MainGame";
        PendingWorldName = worldName;
    }

    public void Consume()
    {
        ShouldLoadOnStart = false;
    }

    public int GetNextAvailableSlot()
    {
        for (int i = 0; i < MaxSlots; i++)
        {
            if (!SlotExists(i)) return i;
        }
        return -1;
    }

    public SaveSlotPreview[] GetAllSlots()
    {
        var slots = new SaveSlotPreview[MaxSlots];
        for (int i = 0; i < MaxSlots; i++)
        {
            slots[i] = SaveSlotReader.ReadSlot(i); // null = empty slot
        }
        return slots;
    }

    public bool SlotExists(int slotIndex)
    {
        return SaveSlotReader.ReadSlot(slotIndex) != null;
    }
}
