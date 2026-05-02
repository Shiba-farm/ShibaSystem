using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static readonly string fileName = "save01.json";
    private static readonly string filePath =
        Path.Combine(Application.persistentDataPath, fileName);

    // บอก GameScene ว่าตอนเข้าไปให้ Load จากเซฟเลยไหม
    public static bool LoadOnStart { get; set; }

    public static void Save(SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, json);
        Debug.Log("[SaveSystem] Saved to " + filePath);
    }

    public static SaveData Load()
    {
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("[SaveSystem] Save file not found");
            return null;
        }

        string json = File.ReadAllText(filePath);
        var data = JsonUtility.FromJson<SaveData>(json);
        return data;
    }

    public static bool SaveExists() => File.Exists(filePath);

    public static void DeleteSave()
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}
