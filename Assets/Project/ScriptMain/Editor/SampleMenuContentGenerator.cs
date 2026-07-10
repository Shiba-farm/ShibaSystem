#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// สร้าง ScriptableObject ที่จำเป็นทั้งหมดให้ระบบเมนูใหม่ทำงานได้ทันที — Signals,
/// Database ว่าง ๆ, EquipmentSlotConfig, MapBounds, และข้อมูลตัวอย่าง (เควส/NPC/
/// สกิล/ของสะสม) เพื่อให้ทดสอบ UI ได้จริงโดยไม่ต้องสร้าง asset มือเองทั้งหมด
///
/// Menu: Tools/Shiba Farm/Menu System/Generate Sample Menu Content
///
/// รันได้หลายครั้ง — จะไม่สร้างซ้ำถ้า asset ที่ path เดียวกันมีอยู่แล้ว (ใช้
/// AssetDatabase.LoadAssetAtPath เช็กก่อนสร้างเสมอ)
/// </summary>
public static class SampleMenuContentGenerator
{
    private const string SignalsPath = "Assets/Project/ScriptableObjects/Signals";
    private const string EquipmentPath = "Assets/Project/ScriptableObjects/Equipment";
    private const string QuestPath = "Assets/Project/ScriptableObjects/Quest";
    private const string NpcPath = "Assets/Project/ScriptableObjects/NPC";
    private const string SkillPath = "Assets/Project/ScriptableObjects/Skill";
    private const string AchievementPath = "Assets/Project/ScriptableObjects/Achievement";
    private const string MapPath = "Assets/Project/ScriptableObjects/Map";

    [MenuItem("Tools/Shiba Farm/Menu System/Generate Sample Menu Content")]
    public static void GenerateAll()
    {
        EnsureFolder(SignalsPath);
        EnsureFolder(EquipmentPath);
        EnsureFolder(QuestPath);
        EnsureFolder(NpcPath);
        EnsureFolder(SkillPath);
        EnsureFolder(AchievementPath);
        EnsureFolder(MapPath);

        GenerateSignals();
        GenerateEquipmentConfig();
        GenerateMapBounds();
        var quests = GenerateSampleQuests();
        var npcs = GenerateSampleNpcs();
        var skills = GenerateSampleSkills();
        var collectibles = GenerateSampleCollectibles();

        GenerateOrUpdate<QuestDatabaseSO>($"{QuestPath}/QuestDatabase.asset", db =>
            SetListField(db, "allQuests", quests));
        GenerateOrUpdate<NPCDatabaseSO>($"{NpcPath}/NPCDatabase.asset", db =>
            SetListField(db, "allNpcs", npcs));
        GenerateOrUpdate<SkillDatabaseSO>($"{SkillPath}/SkillDatabase.asset", db =>
            SetListField(db, "allSkills", skills));
        GenerateOrUpdate<CollectibleDatabaseSO>($"{AchievementPath}/CollectibleDatabase.asset", db =>
            SetListField(db, "allCollectibles", collectibles));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SampleMenuContentGenerator] เสร็จแล้ว — ดูที่ Assets/Project/ScriptableObjects/ (Signals, Equipment, Quest, NPC, Skill, Achievement, Map)");
    }

    // ── Signals ──────────────────────────────────────────────────────────────
    private static void GenerateSignals()
    {
        GenerateOrUpdate<EquipmentDataSignal>($"{SignalsPath}/EquipmentDataSignal.asset", null);
        GenerateOrUpdate<QuestDataSignal>($"{SignalsPath}/QuestDataSignal.asset", null);
        GenerateOrUpdate<RelationshipDataSignal>($"{SignalsPath}/RelationshipDataSignal.asset", null);
        GenerateOrUpdate<SkillDataSignal>($"{SignalsPath}/SkillDataSignal.asset", null);
        GenerateOrUpdate<AchievementDataSignal>($"{SignalsPath}/AchievementDataSignal.asset", null);
        GenerateOrUpdate<LocalPlayerStatSignal>($"{SignalsPath}/LocalPlayerStatSignal.asset", null);
    }

    private static void GenerateEquipmentConfig()
    {
        GenerateOrUpdate<EquipmentSlotConfigSO>($"{EquipmentPath}/EquipmentSlotConfig.asset", config =>
        {
            config.slots = new List<EquipmentSlotConfigSO.SlotEntry>
            {
                new() { slot = EquipSlot.Helmet, displayName = "Helmet" },
                new() { slot = EquipSlot.Ring,   displayName = "Ring" },
                new() { slot = EquipSlot.Shield, displayName = "Shield" },
                new() { slot = EquipSlot.Boots,  displayName = "Boots" },
            };
        });
    }

    private static void GenerateMapBounds()
    {
        GenerateOrUpdate<MapBoundsSO>($"{MapPath}/MapBounds.asset", bounds =>
        {
            bounds.worldMin = new Vector2(-100, -100);
            bounds.worldMax = new Vector2(100, 100);
        });
    }

    // ── Sample Quests ────────────────────────────────────────────────────────
    private static List<QuestDefinitionSO> GenerateSampleQuests()
    {
        ItemSO sampleReward = FindAnyItem();
        var list = new List<QuestDefinitionSO>();

        list.Add(GenerateOrUpdate<QuestDefinitionSO>($"{QuestPath}/Quest_PayFirstDebt.asset", q =>
        {
            q.questId = 1001;
            q.title = "ใช้หนี้งวดแรก";
            q.category = QuestCategory.Main;
            q.description = "เก็บเงินให้ครบตามจำนวนที่เจ้าหนี้เรียกร้องในสิ้นเดือนนี้";
            q.targetProgress = 1;
            if (sampleReward != null) q.rewards = new List<QuestRewardEntry> { new() { item = sampleReward, amount = 5 } };
        }));

        list.Add(GenerateOrUpdate<QuestDefinitionSO>($"{QuestPath}/Quest_MeetTheNeighbors.asset", q =>
        {
            q.questId = 1002;
            q.title = "ทำความรู้จักคนในเกาะ";
            q.category = QuestCategory.Main;
            q.description = "ไปพูดคุยกับชาวบ้านในเกาะอย่างน้อย 3 คน";
            q.targetProgress = 3;
            q.prerequisiteQuests = new List<QuestDefinitionSO> { GetExisting<QuestDefinitionSO>($"{QuestPath}/Quest_PayFirstDebt.asset") };
        }));

        list.Add(GenerateOrUpdate<QuestDefinitionSO>($"{QuestPath}/Quest_CatchThreeFish.asset", q =>
        {
            q.questId = 2001;
            q.title = "ตกปลาสามตัว";
            q.category = QuestCategory.Side;
            q.description = "ลองออกไปตกปลาดูสักสามตัว ไม่ต้องรีบ — ทำได้ทุกเมื่อ";
            q.targetProgress = 3;
            if (sampleReward != null) q.rewards = new List<QuestRewardEntry> { new() { item = sampleReward, amount = 2 } };
        }));

        return list;
    }

    // ── Sample NPCs ──────────────────────────────────────────────────────────
    private static List<NPCDefinitionSO> GenerateSampleNpcs()
    {
        ItemSO sampleGift = FindAnyItem();
        var list = new List<NPCDefinitionSO>();

        list.Add(GenerateOrUpdate<NPCDefinitionSO>($"{NpcPath}/NPC_Buny.asset", n =>
        {
            n.npcId = 1;
            n.displayName = "Buny";
            n.biography = "เพื่อนบ้านใจดีที่อยู่ฟาร์มข้าง ๆ ชอบช่วยน้องชิบะอยู่เสมอ";
            n.maxHeartLevel = 6;
            if (sampleGift != null) n.favoriteGifts = new List<ItemSO> { sampleGift };
        }));

        list.Add(GenerateOrUpdate<NPCDefinitionSO>($"{NpcPath}/NPC_Merchant.asset", n =>
        {
            n.npcId = 2;
            n.displayName = "พ่อค้าเร่";
            n.biography = "เดินทางมาขายของแปลก ๆ ในเกาะเป็นประจำทุกสัปดาห์";
            n.maxHeartLevel = 6;
        }));

        return list;
    }

    // ── Sample Skills ────────────────────────────────────────────────────────
    private static List<SkillDefinitionSO> GenerateSampleSkills()
    {
        var list = new List<SkillDefinitionSO>
        {
            GenerateOrUpdate<SkillDefinitionSO>($"{SkillPath}/Skill_QuickHarvest.asset", s =>
            {
                s.skillId = 101; s.category = SkillCategory.Farming;
                s.displayName = "Quick harvest"; s.description = "Reduces harvest time per level";
                s.maxLevel = 5; s.skillPointCostPerLevel = 2;
            }),
            GenerateOrUpdate<SkillDefinitionSO>($"{SkillPath}/Skill_BumperCrop.asset", s =>
            {
                s.skillId = 102; s.category = SkillCategory.Farming;
                s.displayName = "Bumper crop"; s.description = "Chance to harvest double yield";
                s.maxLevel = 5; s.skillPointCostPerLevel = 3;
            }),
            GenerateOrUpdate<SkillDefinitionSO>($"{SkillPath}/Skill_PatientAngler.asset", s =>
            {
                s.skillId = 201; s.category = SkillCategory.Fishing;
                s.displayName = "Patient angler"; s.description = "Increases rare fish bite chance";
                s.maxLevel = 5; s.skillPointCostPerLevel = 2;
            }),
            GenerateOrUpdate<SkillDefinitionSO>($"{SkillPath}/Skill_DeepDigger.asset", s =>
            {
                s.skillId = 301; s.category = SkillCategory.Mining;
                s.displayName = "Deep digger"; s.description = "Increases chance of rare ore";
                s.maxLevel = 5; s.skillPointCostPerLevel = 2;
            }),
            GenerateOrUpdate<SkillDefinitionSO>($"{SkillPath}/Skill_EfficientCrafting.asset", s =>
            {
                s.skillId = 401; s.category = SkillCategory.Crafting;
                s.displayName = "Efficient crafting"; s.description = "Reduces crafting material cost";
                s.maxLevel = 5; s.skillPointCostPerLevel = 3;
            }),
        };

        // Soil mastery ต้องมี Quick harvest Lv.3 ก่อน — ตัวอย่าง prerequisite chain
        list.Add(GenerateOrUpdate<SkillDefinitionSO>($"{SkillPath}/Skill_SoilMastery.asset", s =>
        {
            s.skillId = 103; s.category = SkillCategory.Farming;
            s.displayName = "Soil mastery"; s.description = "Requires Quick harvest Lv. 3";
            s.maxLevel = 3; s.skillPointCostPerLevel = 4;
            s.requiredSkill = GetExisting<SkillDefinitionSO>($"{SkillPath}/Skill_QuickHarvest.asset");
            s.requiredSkillLevel = 3;
        }));

        return list;
    }

    // ── Sample Collectibles ──────────────────────────────────────────────────
    private static List<CollectibleDefinitionSO> GenerateSampleCollectibles()
    {
        return new List<CollectibleDefinitionSO>
        {
            GenerateOrUpdate<CollectibleDefinitionSO>($"{AchievementPath}/Collectible_Carp.asset", c =>
            { c.collectibleId = 1; c.category = CollectibleCategory.Fish; c.displayName = "Carp"; c.rarity = CollectibleRarity.Common; }),
            GenerateOrUpdate<CollectibleDefinitionSO>($"{AchievementPath}/Collectible_Catfish.asset", c =>
            { c.collectibleId = 2; c.category = CollectibleCategory.Fish; c.displayName = "Catfish"; c.rarity = CollectibleRarity.Common; }),
            GenerateOrUpdate<CollectibleDefinitionSO>($"{AchievementPath}/Collectible_GoldenKoi.asset", c =>
            { c.collectibleId = 3; c.category = CollectibleCategory.Fish; c.displayName = "Golden koi"; c.rarity = CollectibleRarity.Rare; }),
            GenerateOrUpdate<CollectibleDefinitionSO>($"{AchievementPath}/Collectible_Eel.asset", c =>
            { c.collectibleId = 4; c.category = CollectibleCategory.Fish; c.displayName = "Eel"; c.rarity = CollectibleRarity.Uncommon; }),
            GenerateOrUpdate<CollectibleDefinitionSO>($"{AchievementPath}/Collectible_CrimsonTuna.asset", c =>
            { c.collectibleId = 5; c.category = CollectibleCategory.Fish; c.displayName = "Crimson tuna"; c.rarity = CollectibleRarity.Legendary; }),
            GenerateOrUpdate<CollectibleDefinitionSO>($"{AchievementPath}/Collectible_CopperOre.asset", c =>
            { c.collectibleId = 6; c.category = CollectibleCategory.OreGems; c.displayName = "Copper ore"; c.rarity = CollectibleRarity.Common; }),
            GenerateOrUpdate<CollectibleDefinitionSO>($"{AchievementPath}/Collectible_Tomato.asset", c =>
            { c.collectibleId = 7; c.category = CollectibleCategory.Crops; c.displayName = "Tomato"; c.rarity = CollectibleRarity.Common; }),
            GenerateOrUpdate<CollectibleDefinitionSO>($"{AchievementPath}/Collectible_IronSword.asset", c =>
            { c.collectibleId = 8; c.category = CollectibleCategory.CraftedItems; c.displayName = "Iron sword"; c.rarity = CollectibleRarity.Uncommon; }),
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static T GenerateOrUpdate<T>(string assetPath, System.Action<T> configure) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
        }

        configure?.Invoke(asset);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static T GetExisting<T>(string assetPath) where T : ScriptableObject =>
        AssetDatabase.LoadAssetAtPath<T>(assetPath);

    private static void SetListField<TDb, TElement>(TDb database, string fieldName, List<TElement> values) where TDb : ScriptableObject
    {
        var field = typeof(TDb).GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        field?.SetValue(database, values);
    }

    private static ItemSO FindAnyItem()
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemSO");
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private static void EnsureFolder(string fullPath)
    {
        if (AssetDatabase.IsValidFolder(fullPath)) return;

        string[] parts = fullPath.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
