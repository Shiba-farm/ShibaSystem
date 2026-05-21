using UnityEngine;

public class GlobalSaveContext : MonoBehaviour
{
    public static GlobalSaveContext Instance { get; private set; }

    public bool ShouldLoadOnStart { get; private set; } = false;
    public int TargetSlot { get; private set; } = 0;   // which slot to load

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RequestLoad(int slotIndex)
    {
        ShouldLoadOnStart = true;
        TargetSlot = slotIndex;
    }

    public void RequestNewGame(int slotIndex)
    {
        ShouldLoadOnStart = false;
        TargetSlot = slotIndex;   // still need to know which slot to SAVE into later
    }

    public void Consume()
    {
        ShouldLoadOnStart = false;
    }
}
