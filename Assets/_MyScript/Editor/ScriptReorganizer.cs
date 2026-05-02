// =============================================================
//  ScriptReorganizer.cs  —  Unity Editor Tool
//  เปิดจากเมนู: Tools > Shiba Farm > Reorganize Scripts
//
//  จัดโฟลเดอร์ script ใหม่ให้เป็นระเบียบ
//  ใช้ AssetDatabase.MoveAsset() เพื่อไม่ให้ .meta หาย
//  reference ใน Scene/Prefab จะไม่เสีย
// =============================================================
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ScriptReorganizer : EditorWindow
{
    // ===== โครงสร้างข้อมูลสำหรับแต่ละไฟล์ =====
    struct MoveEntry
    {
        public string from; // path เดิม (เริ่มจาก Assets/)
        public string to;   // path ใหม่ (เริ่มจาก Assets/)
    }

    private Vector2 scrollPos;
    private List<MoveEntry> entries;
    private bool previewMode = true;
    private string logText = "";

    [MenuItem("Tools/Shiba Farm/Reorganize Scripts")]
    static void ShowWindow()
    {
        var win = GetWindow<ScriptReorganizer>("Script Reorganizer");
        win.minSize = new Vector2(700, 500);
        win.BuildEntries();
    }

    void BuildEntries()
    {
        entries = new List<MoveEntry>();
        string root = "Assets/_MyScript";

        // ============================
        //  Core / GameManager
        // ============================
        Add($"{root}/GameManager/GameManager.cs",       $"{root}/Core/GameManager/GameManager.cs");
        Add($"{root}/GameManager/ItemDatabase.cs",      $"{root}/Core/ItemDatabase/ItemDatabase.cs");
        Add($"{root}/GameManager/SaveSystem.cs",        $"{root}/Core/SaveSystem/SaveSystem.cs");
        Add($"{root}/GameManager/SaveData.cs",          $"{root}/Core/SaveSystem/SaveData.cs");

        // ============================
        //  Player
        // ============================
        Add($"{root}/PlayerControll/PlayerController.cs",  $"{root}/Player/Controller/PlayerController.cs");
        Add($"{root}/PlayerControll/PlayerEquipment.cs",   $"{root}/Player/Equipment/PlayerEquipment.cs");
        Add($"{root}/PlayerScript/PlayerEnergy.cs",        $"{root}/Player/Energy/PlayerEnergy.cs");
        Add($"{root}/PlayerScript/PlayerPickup.cs",        $"{root}/Player/Pickup/PlayerPickup.cs");
        Add($"{root}/PlayerScript/Pickupable.cs",          $"{root}/Player/Pickup/Pickupable.cs");
        Add($"{root}/PlayerScript/ToolUsage.cs",           $"{root}/Player/Tools/ToolUsage.cs");

        // ============================
        //  Farming
        // ============================
        Add($"{root}/PlayerScript/FarmingSystem.cs",       $"{root}/Farming/FarmingSystem.cs");
        Add($"{root}/Crop/SoilTile.cs",                   $"{root}/Farming/Soil/SoilTile.cs");
        Add($"{root}/Crop/SoilGridSpawner.cs",            $"{root}/Farming/Soil/SoilGridSpawner.cs");
        Add($"{root}/Crop/CropSO.cs",                     $"{root}/Farming/Data/CropSO.cs");
        Add($"{root}/Crop/FarmCameraFollow.cs",            $"{root}/Farming/Camera/FarmCameraFollow.cs");
        Add($"{root}/CursurVisaul/TileCursor.cs",         $"{root}/Farming/UI/TileCursor.cs");

        // ============================
        //  Time System
        // ============================
        Add($"{root}/Time_System/TimeOfDaySystem.cs",     $"{root}/TimeSystem/TimeOfDaySystem.cs");
        Add($"{root}/Time_System/CalendarSystem.cs",      $"{root}/TimeSystem/CalendarSystem.cs");
        Add($"{root}/Time_System/CalendarUI.cs",          $"{root}/TimeSystem/UI/CalendarUI.cs");
        Add($"{root}/Time_System/TimeOfDayUI.cs",         $"{root}/TimeSystem/UI/TimeOfDayUI.cs");
        Add($"{root}/Time_System/MonthlyDebtManager.cs",  $"{root}/TimeSystem/Debt/MonthlyDebtManager.cs");
        Add($"{root}/Time_System/DayNightMusicManager.cs",$"{root}/TimeSystem/Audio/DayNightMusicManager.cs");
        Add($"{root}/Time_System/AutomaticLamp.cs",       $"{root}/TimeSystem/Environment/AutomaticLamp.cs");

        // ============================
        //  NPC & Dialogue
        // ============================
        Add($"{root}/NPC/DialogueManager.cs",             $"{root}/NPC/Dialogue/DialogueManager.cs");
        Add($"{root}/NPC/NPCInteractable.cs",             $"{root}/NPC/Interaction/NPCInteractable.cs");
        Add($"{root}/NPC/DialogueSO.cs",                  $"{root}/NPC/Data/DialogueSO.cs");

        // ============================
        //  Inventory & Hotbar
        // ============================
        Add($"{root}/Ui/InventoryUI.cs",                  $"{root}/Inventory/InventoryUI.cs");
        Add($"{root}/Ui/InventorySlot.cs",                $"{root}/Inventory/InventorySlot.cs");
        Add($"{root}/Ui/InventorySlotClick.cs",           $"{root}/Inventory/InventorySlotClick.cs");
        Add($"{root}/Ui/InventoryDragHandler.cs",         $"{root}/Inventory/InventoryDragHandler.cs");
        Add($"{root}/Ui/InventoryToggle.cs",              $"{root}/Inventory/InventoryToggle.cs");
        Add($"{root}/Ui/HotbarUI.cs",                     $"{root}/Inventory/Hotbar/HotbarUI.cs");
        Add($"{root}/Ui/HotbarSlot.cs",                   $"{root}/Inventory/Hotbar/HotbarSlot.cs");
        Add($"{root}/Ui/HotbarSlotClick.cs",              $"{root}/Inventory/Hotbar/HotbarSlotClick.cs");

        // ============================
        //  Economy & Shop
        // ============================
        Add($"{root}/Wallet/PlayerWallet.cs",             $"{root}/Economy/PlayerWallet.cs");
        Add($"{root}/Wallet/MoneyUI.cs",                  $"{root}/Economy/UI/MoneyUI.cs");
        Add($"{root}/Wallet/SellBox.cs",                  $"{root}/Economy/Sell/SellBox.cs");
        Add($"{root}/Wallet/ReadOnlyAttribute.cs",        $"{root}/Utility/Attributes/ReadOnlyAttribute.cs");
        Add($"{root}/Wallet/Shop/ShopUI.cs",              $"{root}/Economy/Shop/ShopUI.cs");
        Add($"{root}/Wallet/Shop/ShopDefinition.cs",      $"{root}/Economy/Shop/ShopDefinition.cs");
        Add($"{root}/Wallet/Shop/ShopCategory.cs",        $"{root}/Economy/Shop/ShopCategory.cs");
        Add($"{root}/Wallet/Shop/ShopItemView.cs",        $"{root}/Economy/Shop/ShopItemView.cs");
        Add($"{root}/Wallet/Shop/ShopTabButton.cs",       $"{root}/Economy/Shop/ShopTabButton.cs");
        Add($"{root}/Wallet/Shop/ShopTabsBar.cs",         $"{root}/Economy/Shop/ShopTabsBar.cs");
        Add($"{root}/Wallet/Shop/ShopTrigger.cs",         $"{root}/Economy/Shop/ShopTrigger.cs");

        // ============================
        //  Items
        // ============================
        Add($"{root}/PlayerControll/ItemSO/ItemSO.cs",           $"{root}/Items/ItemSO.cs");
        Add($"{root}/PlayerControll/ItemSO/ChoppableCut_Tree.cs",$"{root}/Items/Interactable/ChoppableCut_Tree.cs");
        Add($"{root}/PlayerControll/ItemSO/ItemMagnet.cs",       $"{root}/Items/Pickup/ItemMagnet.cs");

        // ============================
        //  UI & Menu
        // ============================
        Add($"{root}/GameManager/MainMenuUI.cs",          $"{root}/UI/Menu/MainMenuUI.cs");
        Add($"{root}/GameManager/PauseMenuUI.cs",         $"{root}/UI/Menu/PauseMenuUI.cs");
        Add($"{root}/GameManager/AudioSettingsUI.cs",     $"{root}/UI/Settings/AudioSettingsUI.cs");

        // ============================
        //  Rest & Recovery
        // ============================
        Add($"{root}/BedRest.cs",                         $"{root}/Rest/BedRest.cs");
        Add($"{root}/BedRestAdvanced.cs",                 $"{root}/Rest/BedRestAdvanced.cs");

        // ============================
        //  Utility / Misc
        // ============================
        Add($"{root}/CinematicFreeCam.cs",                $"{root}/Utility/CinematicFreeCam.cs");
        // TestOdin.cs ไม่ย้าย (ลบได้)
    }

    void Add(string from, string to)
    {
        entries.Add(new MoveEntry { from = from, to = to });
    }

    // ===== GUI =====
    void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Shiba Farm — Script Reorganizer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "เครื่องมือนี้จะย้ายไฟล์ script ไปยังโฟลเดอร์ใหม่ที่จัดหมวดหมู่ดีขึ้น\n" +
            "ใช้ AssetDatabase.MoveAsset() จึงไม่ทำให้ reference เสีย\n\n" +
            "ขั้นตอน:\n" +
            "1. กด 'Preview' เพื่อดูรายการที่จะย้าย\n" +
            "2. ตรวจสอบว่าถูกต้อง\n" +
            "3. กด 'Execute Move' เพื่อย้ายจริง",
            MessageType.Info);

        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Preview (ดูก่อน)", GUILayout.Height(30)))
        {
            previewMode = true;
            RunMove(dryRun: true);
        }
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Execute Move (ย้ายจริง)", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog(
                "ยืนยันการย้ายไฟล์",
                "คุณแน่ใจหรือไม่ว่าต้องการย้ายไฟล์ทั้งหมด?\n\n" +
                "แนะนำ: commit git ก่อนย้าย เพื่อความปลอดภัย",
                "ย้ายเลย", "ยกเลิก"))
            {
                previewMode = false;
                RunMove(dryRun: false);
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        // Log area
        EditorGUILayout.LabelField(previewMode ? "Preview:" : "Result:", EditorStyles.boldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
        EditorGUILayout.TextArea(logText, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    void RunMove(bool dryRun)
    {
        if (entries == null) BuildEntries();

        logText = "";
        int moved = 0, skipped = 0, errors = 0;

        foreach (var e in entries)
        {
            // ตรวจไฟล์ต้นทาง
            if (!File.Exists(e.from))
            {
                logText += $"[SKIP] ไม่พบไฟล์: {e.from}\n";
                skipped++;
                continue;
            }

            // ถ้า from == to ข้าม
            if (e.from == e.to)
            {
                logText += $"[SKIP] อยู่ที่เดิมแล้ว: {e.from}\n";
                skipped++;
                continue;
            }

            // สร้างโฟลเดอร์ปลายทาง
            string destDir = Path.GetDirectoryName(e.to);
            if (!dryRun && !string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
                // สร้าง .meta สำหรับโฟลเดอร์ใหม่
                AssetDatabase.Refresh();
            }

            if (dryRun)
            {
                logText += $"[MOVE] {e.from}\n     → {e.to}\n\n";
                moved++;
            }
            else
            {
                string result = AssetDatabase.MoveAsset(e.from, e.to);
                if (string.IsNullOrEmpty(result))
                {
                    logText += $"[OK] {e.from}\n   → {e.to}\n\n";
                    moved++;
                }
                else
                {
                    logText += $"[ERROR] {e.from}\n   → {result}\n\n";
                    errors++;
                }
            }
        }

        logText += "─────────────────────────────────\n";
        logText += $"สรุป: ย้าย {moved} ไฟล์, ข้าม {skipped}, ผิดพลาด {errors}\n";

        if (!dryRun)
        {
            // ลบโฟลเดอร์เก่าที่ว่างแล้ว
            CleanEmptyFolders("Assets/_MyScript");
            AssetDatabase.Refresh();
            logText += "\nAssetDatabase.Refresh() เรียบร้อย\n";
            logText += "โฟลเดอร์เก่าที่ว่างถูกลบแล้ว\n";
        }
    }

    void CleanEmptyFolders(string path)
    {
        if (!Directory.Exists(path)) return;

        foreach (string dir in Directory.GetDirectories(path))
        {
            CleanEmptyFolders(dir);
        }

        // ถ้าโฟลเดอร์ว่าง (ไม่มีไฟล์ ไม่มีโฟลเดอร์ย่อย) ให้ลบ
        if (Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0)
        {
            // ไม่ลบโฟลเดอร์หลัก _MyScript และ Editor
            string folderName = Path.GetFileName(path);
            if (folderName == "_MyScript" || folderName == "Editor") return;

            string metaFile = path + ".meta";
            if (File.Exists(metaFile))
            {
                FileUtil.DeleteFileOrDirectory(metaFile);
            }
            FileUtil.DeleteFileOrDirectory(path);
            logText += $"[CLEAN] ลบโฟลเดอร์ว่าง: {path}\n";
        }
    }
}
#endif
