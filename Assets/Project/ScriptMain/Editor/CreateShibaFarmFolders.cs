#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// สร้าง folder structure สำหรับ Shiba Farm
/// Menu: Tools/Shiba Farm/Create Project Folder Structure
/// </summary>
public class CreateShibaFarmFolders
{
    static readonly string[] Folders = new[]
    {
        // ── Core Save ──────────────────────────────────────────────────────
        "_Project/Scripts/Core/Save",
        "_Project/Scripts/Core/Network",

        // ── Player ─────────────────────────────────────────────────────────
        "_Project/Scripts/Player",

        // ── Inventory ──────────────────────────────────────────────────────
        "_Project/Scripts/Inventory",

        // ── Items ──────────────────────────────────────────────────────────
        "_Project/Scripts/Items/Base",
        "_Project/Scripts/Items/Tools",
        "_Project/Scripts/Items/Food",

        // ── Gameplay ───────────────────────────────────────────────────────
        "_Project/Scripts/Farming",
        "_Project/Scripts/Dungeon",
        "_Project/Scripts/Enemies/Slime",

        // ── UI & Editor ────────────────────────────────────────────────────
        "_Project/Scripts/UI",
        "_Project/Scripts/Editor",

        // ── Animations ─────────────────────────────────────────────────────
        "_Project/Animations/Player",
        "_Project/Animations/Enemies/Slime",
        "_Project/Animations/Items",

        // ── Audio ──────────────────────────────────────────────────────────
        "_Project/Audio/Music",
        "_Project/Audio/SFX",

        // ── Fonts ──────────────────────────────────────────────────────────
        "_Project/Fonts",

        // ── Materials ──────────────────────────────────────────────────────
        "_Project/Materials/Characters",
        "_Project/Materials/Environment/Farm",
        "_Project/Materials/Environment/Dungeon",
        "_Project/Materials/Items",
        "_Project/Materials/VFX",

        // ── Prefabs ────────────────────────────────────────────────────────
        "_Project/Prefabs/Characters/Player",
        "_Project/Prefabs/Characters/Enemies/Slime",
        "_Project/Prefabs/Environment/Farm/Trees",
        "_Project/Prefabs/Environment/Dungeon",
        "_Project/Prefabs/Items/Tools",
        "_Project/Prefabs/Items/Food",
        "_Project/Prefabs/Items/Pickups",
        "_Project/Prefabs/UI",
        "_Project/Prefabs/Network",

        // ── ScriptableObjects ──────────────────────────────────────────────
        "_Project/ScriptableObjects/Items/Tools",
        "_Project/ScriptableObjects/Items/Food",
        "_Project/ScriptableObjects/Signals",
        "_Project/ScriptableObjects/DungeonConfig",

        // ── Scenes ─────────────────────────────────────────────────────────
        "_Project/Scenes/Main",
        "_Project/Scenes/Farm",
        "_Project/Scenes/Dungeon",
        "_Project/Scenes/UI",

        // ── Shaders ────────────────────────────────────────────────────────
        "_Project/Shaders",

        // ── Textures ───────────────────────────────────────────────────────
        "_Project/Textures/Characters",
        "_Project/Textures/Environment/Farm",
        "_Project/Textures/Environment/Dungeon",
        "_Project/Textures/Items",
        "_Project/Textures/UI",

        // ── UI Assets ──────────────────────────────────────────────────────
        "_Project/UI/Icons",
        "_Project/UI/Sprites",

        // ── SaveData (runtime, gitignored) ─────────────────────────────────
        "_Project/SaveData",

        // ── Third Party ────────────────────────────────────────────────────
        "ThirdParty",
    };

    [MenuItem("Tools/Shiba Farm/Create Project Folder Structure")]
    static void CreateFolders()
    {
        int created = 0;
        int skipped = 0;

        foreach (string folderPath in Folders)
        {
            string fullPath = Path.Combine(Application.dataPath, folderPath);

            if (Directory.Exists(fullPath))
            {
                skipped++;
                continue;
            }

            Directory.CreateDirectory(fullPath);

            // สร้าง .gitkeep เพื่อให้ Git track folder ว่าง
            string gitkeep = Path.Combine(fullPath, ".gitkeep");
            File.WriteAllText(gitkeep, "");

            created++;
            Debug.Log($"[ShibaFarm] สร้างแล้ว: Assets/{folderPath}");
        }

        // แจ้ง Unity ให้ refresh และสร้าง .meta files
        AssetDatabase.Refresh();

        string message = $"สร้าง {created} folders\nข้าม {skipped} folders (มีอยู่แล้ว)";
        Debug.Log($"[ShibaFarm] เสร็จแล้ว — {message}");

        EditorUtility.DisplayDialog(
            "Shiba Farm — Folder Structure",
            message + "\n\nอย่าลืมเพิ่ม Assets/_Project/SaveData/ ใน .gitignore",
            "OK"
        );
    }

    // ── Optional: เพิ่ม SaveData ใน .gitignore อัตโนมัติ ──────────────────
    [MenuItem("Tools/Shiba Farm/Add SaveData to .gitignore")]
    static void AddSaveDataToGitignore()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string gitignorePath = Path.Combine(projectRoot, ".gitignore");

        string entry = "\n# Shiba Farm runtime save data\nAssets/_Project/SaveData/\n";

        if (!File.Exists(gitignorePath))
        {
            File.WriteAllText(gitignorePath, entry.TrimStart());
            Debug.Log("[ShibaFarm] สร้าง .gitignore ใหม่และเพิ่ม SaveData entry");
        }
        else
        {
            string content = File.ReadAllText(gitignorePath);
            if (content.Contains("SaveData/"))
            {
                EditorUtility.DisplayDialog("Shiba Farm", "SaveData/ มีอยู่ใน .gitignore แล้ว", "OK");
                return;
            }
            File.AppendAllText(gitignorePath, entry);
            Debug.Log("[ShibaFarm] เพิ่ม SaveData entry ใน .gitignore แล้ว");
        }

        EditorUtility.DisplayDialog(
            "Shiba Farm — .gitignore",
            "เพิ่ม Assets/_Project/SaveData/ ใน .gitignore เรียบร้อย",
            "OK"
        );
    }
}
#endif