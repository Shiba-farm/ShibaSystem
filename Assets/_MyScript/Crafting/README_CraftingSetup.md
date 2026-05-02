# Crafting & Punishment System — Setup Guide
# ระบบคราฟ + ระบบลงโทษ — คู่มือตั้งค่าใน Unity

---

## สรุปไฟล์ทั้งหมดที่สร้าง

| ไฟล์ | ที่อยู่ | หน้าที่ |
|------|---------|---------|
| `CraftingRecipeSO.cs` | `Crafting/Data/` | ScriptableObject สูตรคราฟ |
| `FarmHelperSO.cs` | `Crafting/Data/` | ScriptableObject ตัวช่วยฟาร์ม |
| `FarmHelper.cs` | `Crafting/` | MonoBehaviour บน Prefab ตัวช่วยที่วางในโลก |
| `FarmHelperManager.cs` | `Crafting/` | Singleton จัดการตัวช่วยทั้งหมด |
| `CraftingManager.cs` | `Crafting/` | Singleton ระบบคราฟ core logic |
| `CraftingUI.cs` | `Crafting/` | UI โต๊ะคราฟ |
| `WorkbenchInteraction.cs` | `Crafting/` | ใส่บน Workbench ให้ผู้เล่นกด E เพื่อเปิด |
| `DebtPunishmentSystem.cs` | `TimeSystem/Debt/` | ระบบลงโทษเมื่อไม่จ่ายหนี้ |

---

## STEP 1: สร้าง Managers

1. สร้าง **Empty GameObject** ชื่อ `CraftingManager`
2. Add Component → `CraftingManager.cs`
3. สร้าง **Empty GameObject** ชื่อ `FarmHelperManager`
4. Add Component → `FarmHelperManager.cs`
5. สร้าง **Empty GameObject** ชื่อ `DebtPunishmentSystem` (หรือใส่บน GameObject เดียวกับ DebtCollectorManager ก็ได้)
6. Add Component → `DebtPunishmentSystem.cs`
   - ลาก `DebtCollectorManager` ไปใส่ช่อง `Debt Manager`

---

## STEP 2: สร้างวัตถุดิบ (CraftingMaterial Items)

### ตัวอย่างวัตถุดิบ

ไปที่ `Assets/_MyScript/Items/` → Right-Click → Create → Items → Item

| ชื่อ | Category | แหล่งหา |
|------|----------|---------|
| ไม้ (Wood) | CraftingMaterial | ตัดต้นไม้ (Forest) |
| หิน (Stone) | CraftingMaterial | ขุดแร่ (Mine/River) |
| เหล็ก (Iron Ore) | CraftingMaterial | ขุดแร่ (Mine) |
| เชือก (Rope) | CraftingMaterial | คราฟจากไม้ / ซื้อจากร้าน |
| ผ้า (Cloth) | CraftingMaterial | ซื้อจากร้าน / Quest reward |
| แก้ว (Glass) | CraftingMaterial | ขุดทราย → คราฟ |
| ท่อ (Pipe) | CraftingMaterial | คราฟจากเหล็ก |
| ตะปู (Nail) | CraftingMaterial | คราฟจากเหล็ก |

สำหรับแต่ละ ItemSO:
- ตั้ง `category` = **CraftingMaterial**
- ตั้ง `isStackable` = true
- ตั้ง `maxStack` = 99
- ใส่ icon

---

## STEP 3: สร้าง FarmHelper SO (ตัวช่วยฟาร์ม)

ไปที่ `Assets/` → Right-Click → Create → Crafting → Farm Helper

### ตัวช่วย 6 ชนิดที่ออกแบบไว้

#### 1. บัวรดน้ำอัตโนมัติ (Auto Sprinkler)
| Field | Value |
|-------|-------|
| helperName | บัวรดน้ำอัตโนมัติ |
| description | รดน้ำแปลงผักรอบ ๆ ทุกเช้าอัตโนมัติ |
| effectType | **AutoWater** |
| effectRadius | 3 (ครอบคลุม 3 tile รอบ ๆ) |
| effectValue | 1 |
| durabilityDays | 30 (ต้องซ่อมทุก 30 วัน) |
| destructionPriority | **5** (สูงสุด — ลูกน้องเจ้าหนี้ทำลายก่อน!) |

#### 2. หุ่นไล่กา (Scarecrow)
| Field | Value |
|-------|-------|
| helperName | หุ่นไล่กา |
| description | ป้องกันนกมากินเมล็ดพันธุ์ |
| effectType | **Scarecrow** |
| effectRadius | 4 |
| effectValue | 1 |
| durabilityDays | -1 (ไม่เสื่อม) |
| destructionPriority | 2 |

#### 3. เครื่องให้ปุ๋ย (Fertilizer Station)
| Field | Value |
|-------|-------|
| helperName | เครื่องให้ปุ๋ย |
| description | เพิ่มความเร็วเติบโตของพืช 50% |
| effectType | **Fertilizer** |
| effectRadius | 2 |
| effectValue | 0.5 (= +50% growth speed) |
| durabilityDays | 20 |
| destructionPriority | **4** |

#### 4. รั้วป้องกัน (Protection Fence)
| Field | Value |
|-------|-------|
| helperName | รั้วป้องกัน |
| description | ลดความเสียหายจากพายุและแมลง |
| effectType | **Fence** |
| effectRadius | 5 |
| effectValue | 0.7 (ลดความเสียหาย 70%) |
| durabilityDays | -1 |
| destructionPriority | 3 |

#### 5. กับดักแมลง (Insect Trap)
| Field | Value |
|-------|-------|
| helperName | กับดักแมลง |
| description | ดักแมลงไม่ให้มากินพืชผล |
| effectType | **InsectTrap** |
| effectRadius | 3 |
| effectValue | 1 |
| durabilityDays | 14 |
| destructionPriority | 1 |

#### 6. กับดักสัตว์ (Animal Trap)
| Field | Value |
|-------|-------|
| helperName | กับดักสัตว์ |
| description | ป้องกันสัตว์ป่าขโมยผลผลิต |
| effectType | **AnimalTrap** |
| effectRadius | 4 |
| effectValue | 1 |
| durabilityDays | 14 |
| destructionPriority | 1 |

---

## STEP 4: สร้าง ItemSO สำหรับตัวช่วย

แต่ละ FarmHelper ต้องมี ItemSO คู่กัน (เพื่อให้ผู้เล่นถือใน Inventory ก่อนวาง)

ตัวอย่าง: สร้าง ItemSO ชื่อ "Auto Sprinkler Item":
- `itemName` = "บัวรดน้ำอัตโนมัติ"
- `category` = **FarmHelper**
- `isStackable` = false
- `maxStack` = 1
- `farmHelperData` = ลาก FarmHelperSO (บัวรดน้ำอัตโนมัติ) มาใส่
- `sellable` = true
- `sellPrice` = 500

ทำเหมือนกันสำหรับตัวช่วยทั้ง 6 ชนิด

---

## STEP 5: สร้าง Crafting Recipes

ไปที่ `Assets/` → Right-Click → Create → Crafting → Recipe

### สูตรตัวอย่าง

#### สูตร: บัวรดน้ำอัตโนมัติ (Auto Sprinkler)
| Field | Value |
|-------|-------|
| recipeName | บัวรดน้ำอัตโนมัติ |
| ingredients | ท่อ x2, เหล็ก x3, แก้ว x1 |
| resultItem | (ItemSO: บัวรดน้ำอัตโนมัติ) |
| resultAmount | 1 |
| requiresLearning | false |
| energyCost | 10 |

#### สูตร: หุ่นไล่กา (Scarecrow)
| ingredients | ไม้ x5, ผ้า x2, เชือก x1 |

#### สูตร: เครื่องให้ปุ๋ย (Fertilizer Station)
| ingredients | ไม้ x3, เหล็ก x2, หิน x2 |

#### สูตร: รั้วป้องกัน (Protection Fence)
| ingredients | ไม้ x8, ตะปู x4, เชือก x2 |

#### สูตร: กับดักแมลง (Insect Trap)
| ingredients | ไม้ x2, เชือก x2, หิน x1 |

#### สูตร: กับดักสัตว์ (Animal Trap)
| ingredients | ไม้ x4, เหล็ก x1, เชือก x3 |

---

## STEP 6: ตั้งค่า CraftingManager

1. เลือก `CraftingManager` ใน Hierarchy
2. ลาก Recipe SO ทั้ง 6 ตัว ไปใส่ช่อง `All Recipes`
3. ลาก InventoryUI ไปใส่ช่อง `Inventory` (หรือจะปล่อยว่าง — จะ FindInstance อัตโนมัติ)

---

## STEP 7: สร้าง Workbench (โต๊ะคราฟ)

1. สร้าง 3D Model โต๊ะคราฟ (หรือใช้ Cube ชั่วคราว)
2. ใส่ไว้ในฟาร์ม/บ้าน
3. Add Component → `WorkbenchInteraction.cs`
   - `interactDistance` = 2.5
   - `interactKey` = E
   - (Optional) สร้าง UI Text "กด E เพื่อคราฟ" ลากไปใส่ `promptUI`

---

## STEP 8: สร้าง Crafting UI ใน Canvas

### โครงสร้าง Canvas:

```
Canvas_Crafting (หรือเพิ่มใน Canvas เดิม)
├── CraftingPanel (Panel) ← ลากไปใส่ CraftingUI.craftingPanel
│   ├── RecipeListPanel (ซ้าย)
│   │   └── ScrollView
│   │       └── Content ← ลากไปใส่ recipeListParent
│   │           └── (RecipeButtonPrefab จะ spawn ที่นี่)
│   │
│   ├── DetailPanel (ขวา)
│   │   ├── SelectedIcon (Image) ← selectedIcon
│   │   ├── SelectedName (TMP) ← selectedNameText
│   │   ├── SelectedDesc (TMP) ← selectedDescText
│   │   ├── IngredientList (Vertical Layout)
│   │   │   └── Content ← ingredientListParent
│   │   ├── ResultText (TMP) ← resultText
│   │   └── FeedbackText (TMP) ← feedbackText
│   │
│   └── ButtonBar
│       ├── CraftButton (Button) ← craftButton
│       │   └── Text (TMP) ← craftButtonText
│       └── CloseButton (Button) ← closeButton
```

### Prefab ที่ต้องสร้าง:

#### RecipeButtonPrefab
```
RecipeButton (Button + Image)
├── Icon (Image) — ชื่อ "Icon"
└── Label (TMP) — TextMeshProUGUI
```

#### IngredientRowPrefab
```
IngredientRow (HorizontalLayout)
├── Icon (Image) — ชื่อ "Icon"
├── Name (TMP) — ชื่อ "Name"
└── Amount (TMP) — ชื่อ "Amount"
```

---

## STEP 9: ตั้งค่า DebtPunishmentSystem

1. เลือก `DebtPunishmentSystem` ใน Hierarchy
2. ลาก `DebtCollectorManager` ไปใส่ช่อง `Debt Manager`
3. ตั้งค่า:
   - `missesBeforeDestruction` = 2 (ไม่จ่าย 2 ครั้งถึงจะทำลาย)
   - `baseDestroyCount` = 1
   - `extraDebtPerDestruction` = 1000
   - `destroyAllAtMax` = true
   - `maxConsecutiveForDestroyAll` = 4

### (Optional) Punishment UI:

```
Canvas_Punishment (หรือเพิ่มใน Canvas เดิม)
├── PunishmentPanel (Panel) ← punishmentPanel
│   ├── DialogueText (TMP) ← punishmentDialogueText
│   └── DetailText (TMP) ← punishmentDetailText
```

---

## STEP 10: อัพเดท Save/Load

ใน GameManager (หรือที่ Save/Load):

```csharp
// === SAVE ===
saveData.farmHelpers = FarmHelperManager.Instance?.GetSaveData();
saveData.learnedRecipes = CraftingManager.Instance?.GetLearnedRecipes();
saveData.consecutiveMisses = DebtPunishmentSystem.Instance?.GetConsecutiveMisses() ?? 0;

// === LOAD ===
if (FarmHelperManager.Instance != null && saveData.farmHelpers != null)
    FarmHelperManager.Instance.ApplySaveData(saveData.farmHelpers, allFarmHelperSOs);
if (CraftingManager.Instance != null)
    CraftingManager.Instance.SetLearnedRecipes(saveData.learnedRecipes);
if (DebtPunishmentSystem.Instance != null)
    DebtPunishmentSystem.Instance.SetConsecutiveMisses(saveData.consecutiveMisses);
```

---

## ระบบลงโทษ — สรุปกฎ

| ครั้งที่ไม่จ่ายติดกัน | ผลลัพธ์ |
|----------------------|---------|
| 1 ครั้ง | คำเตือน + ค่าปรับปกติ (¥500) |
| 2 ครั้ง | **ทำลายตัวช่วย 1 ตัว** + หนี้เพิ่ม ¥1,000 |
| 3 ครั้ง | **ทำลายตัวช่วย 2 ตัว** + หนี้เพิ่ม ¥2,000 |
| 4+ ครั้ง | **ทำลายทั้งหมด** + หนี้เพิ่มตามจำนวน |

**สำคัญ:** ถ้าจ่ายถึงขั้นต่ำ → consecutiveMisses reset เป็น 0

**Priority ทำลาย** (สูง = ถูกทำลายก่อน):
1. บัวรดน้ำอัตโนมัติ (5) ← เป้าหมายแรก!
2. เครื่องให้ปุ๋ย (4)
3. รั้วป้องกัน (3)
4. หุ่นไล่กา (2)
5. กับดักแมลง/สัตว์ (1)

---

## Debug / ทดสอบ

- **DebtPunishmentSystem** → Right-Click Inspector → Debug → Force Punishment
- **DebtCollectorManager** → Right-Click Inspector → Debug → Force Collector Visit
- กด "ไม่จ่าย" 2 ครั้งติด → ลูกน้องเจ้าหนี้ควรมาทำลายตัวช่วย
