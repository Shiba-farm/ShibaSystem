using System.IO;
using UnityEngine;

[System.Serializable]
public class SaveSlotPreview
{
    public string savedAt;
    public string worldName;
    public int    slotIndex;
    public WorldSaveData world;   // enough to show month/day
}

public static class SaveSlotReader
{
    public static SaveSlotPreview ReadSlot(int slot)
    {
        string path = SaveLoadManager.GetSavePathStatic(slot);
        if (!File.Exists(path)) return null;

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<SaveSlotPreview>(json);
    }
}