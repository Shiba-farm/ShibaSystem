# Shiba Farm - Code Review Report

## สรุปภาพรวม

โปรเจกต์ Shiba Farm มีโค้ดเบสที่ดีมาก โครงสร้างแบ่งเป็นระบบชัดเจน ใช้ ScriptableObject อย่างเหมาะสม และมี Singleton pattern สำหรับ manager ต่างๆ โค้ดอ่านง่าย เข้าใจ flow ได้ไม่ยาก

**Scripts ทั้งหมด:** 55 ไฟล์ใน `_MyScript/`  
**ระบบหลัก:** 11 ระบบ (GameManager, Player, Farming, Time, NPC, Inventory, Shop, Wallet, Energy, Save, Rest)

---

## 1. GameManager & Save System

**ไฟล์:** `GameManager.cs`, `SaveSystem.cs`, `SaveData.cs`, `ItemDatabase.cs`

### จุดเด่น
- Singleton pattern ใช้ถูกต้อง มี null check
- SaveSystem เป็น static class เรียบง่าย ใช้ JSON
- SaveData เก็บข้อมูลครบ: ตำแหน่งผู้เล่น, เวลา, energy, inventory, hotbar, soil
- มี Debug Buttons ผ่าน Odin Inspector ช่วย test ได้ง่าย

### สิ่งที่ควรปรับปรุง

**[สำคัญ] SaveData ยังไม่เก็บข้อมูลเงินและหนี้**
```csharp
// ใน SaveData.cs ยังขาด:
public int money;        // <-- ยังไม่มี
public int currentDebt;  // <-- ยังไม่มี
```
ถ้า load game กลับมา เงินกับหนี้จะหาย ต้องเพิ่มใน SaveData และอัพเดต GameManager.SaveGame()/LoadGame()

**[ปานกลาง] FindObjectsOfType<SoilTile>() ใน Save/Load**
```csharp
SoilTile[] tiles = FindObjectsOfType<SoilTile>(); // ช้าถ้า tile เยอะ
```
ควรเก็บ reference ไว้ใน SoilGridSpawner แทน หรือใช้ registry pattern

**[เล็กน้อย] ItemDatabase ค้นหาด้วย foreach**
```csharp
// ตอนนี้เป็น O(n) ทุกครั้ง
foreach (var it in items) { if (it.itemName == name) return it; }
```
ถ้า item เยอะขึ้น ควรใช้ Dictionary<string, ItemSO> แทน

**[เล็กน้อย] Save slot เดียว**
```csharp
private static readonly string fileName = "save01.json"; // มีแค่ slot เดียว
```
ในอนาคตอาจต้องรองรับหลาย save slot

---

## 2. Player Controller & Equipment

**ไฟล์:** `PlayerController.cs`, `PlayerEquipment.cs`

### จุดเด่น
- แยก action handling ออกจาก movement ดี
- ระบบ cache (cachedItem, cachedTile, cachedTree) ป้องกัน race condition ระหว่าง animation
- Animation Event (OnActionImpact, OnActionAnimationFinished) ใช้ถูกวิธี
- FaceTo() ก่อนทำ action ดูเป็นธรรมชาติ

### สิ่งที่ควรปรับปรุง

**[สำคัญ] Dialogue check อยู่หลัง HandleMovement()**
```csharp
void Update() {
    // ...
    HandleMovement();       // <-- เดินได้ก่อน
    HandleActionInput();
    if (DialogueManager.Instance.IsDialogueActive) return; // <-- check ทีหลัง
}
```
ควรย้าย dialogue check ไปก่อน HandleMovement() ไม่งั้นผู้เล่นจะเดินได้ขณะกำลังคุยกับ NPC

**[ปานกลาง] PlayerEquipment เช็คใน Update ทุกเฟรม**
```csharp
void Update() {
    ItemSO selectedItem = HotbarUI.Instance.GetSelectedItem();
    if (selectedItem != currentItem) EquipItem(selectedItem); // เช็คทุกเฟรม
}
```
ควรใช้ event-based แทน (เมื่อ hotbar เปลี่ยน slot ค่อย callback มา)

**[เล็กน้อย] StartFishing มีโครง แต่ยังไม่มีระบบตกปลาจริง**
```csharp
public void StartFishing(Transform fishPoint) { ... } // มีแค่ trigger animation
public void OnFishingAnimationFinished() { isBusyAction = false; } // แค่ reset flag
```
เป็นจุดที่พร้อมต่อยอดระบบตกปลาได้เลย

---

## 3. Farming System & Crops

**ไฟล์:** `FarmingSystem.cs`, `SoilTile.cs`, `CropSO.cs`, `SoilGridSpawner.cs`

### จุดเด่น
- CropSO ออกแบบดีมาก มี growth stages, yield range, requiresWaterEachStage
- SoilTile มีทั้ง save/load support
- FlatPos() helper ดีมาก ป้องกันปัญหา Y ต่างระดับ
- harvest มี delegate pattern (AddToInventory func) ยืดหยุ่นดี

### สิ่งที่ควรปรับปรุง

**[สำคัญ] Crop โตตาม real-time ไม่ใช่ game-time**
```csharp
// ใน SoilTile.Update()
stageTimer += Time.deltaTime; // ใช้ real-time
```
แต่เกมมีระบบเวลาแยก (8 นาที = 1 วัน) ถ้าผู้เล่น pause หรือ time scale เปลี่ยน จะไม่ sync กัน ควรผูกกับ CalendarSystem หรือ TimeOfDaySystem

**[สำคัญ] ปลูกเมล็ดลด amount แต่ไม่เช็ค inventory**
```csharp
// ใน FarmingSystem.PlantSeed()
slot.amount -= 1;
if (slot.amount <= 0) slot.Clear();
```
ลดจาก hotbar slot เท่านั้น ไม่ได้เช็คว่ามี seed เหลือใน inventory ด้วย ซึ่งอาจจะ OK ถ้า design เป็นแบบนั้น แต่ควร confirm

**[ปานกลาง] SellBox ขายหมดทุกอย่าง (sellAllKey)**
```csharp
void SellAllFromInventory() {
    foreach (var s in inv.slots) { ... s.Clear(); } // ขายหมดเลย
}
```
ไม่มีการ confirm กับผู้เล่น ถ้ากดผิดจะขายของหายหมด ควรเพิ่ม confirmation UI หรือแยก sellable/non-sellable

**[เล็กน้อย] CropSO ไม่มีระบบ season**
```csharp
// ยังไม่มี field แบบนี้:
public Season[] growableSeasons; // ปลูกได้เฉพาะฤดูไหน
```
ต้องเพิ่มเมื่อทำระบบฤดูกาล

---

## 4. Time / Calendar / Debt System

**ไฟล์:** `TimeOfDaySystem.cs`, `CalendarSystem.cs`, `MonthlyDebtManager.cs`

### จุดเด่น
- TimeOfDaySystem สวยมาก มี sun/moon, skybox swap, phase events
- CalendarSystem ติดตาม cross-midnight ดี
- MonthlyDebtManager ใช้ event OnDayEnded ดีมาก ไม่ต้อง poll
- มี late fee ถ้าจ่ายไม่ครบ

### สิ่งที่ควรปรับปรุง

**[สำคัญ] ยังไม่มีระบบฤดูกาล (Season)**
CalendarSystem มี month แต่ยังไม่มี Season enum หรือ logic ที่แปลงเดือนเป็นฤดู เช่น:
```csharp
public enum Season { Spring, Summer, Fall, Winter }
public Season CurrentSeason => (Season)((month - 1) / 3); // ตัวอย่าง
```

**[สำคัญ] ระบบลงโทษยังไม่มี**
```csharp
// ใน MonthlyDebtManager.OnDayEnded:
currentDebt = currentDebt - have + lateFee; // แค่เพิ่มหนี้
// ยังไม่มี: ยึดของ, ทำลายของในบ้าน, ลดระดับอุปกรณ์
```

**[ปานกลาง] OnDateChanged fire ทุกเฟรม**
```csharp
// ใน CalendarSystem.Update() -> TickFromTOD()
OnDateChanged?.Invoke(date); // fire ทุก frame!
```
ถ้ามี listener หลายตัว จะเปลืองทรัพยากร ควรเปลี่ยนเป็น fire เฉพาะเมื่อ minute เปลี่ยน

**[เล็กน้อย] Debt ไม่ save**
ถ้า load game หนี้จะ reset เป็น startingDebt

---

## 5. Inventory & Shop

**ไฟล์:** `InventoryUI.cs`, `InventorySlot.cs`, `HotbarUI.cs`, `ShopUI.cs`, `ShopDefinition.cs`

### จุดเด่น
- Inventory รองรับ stacking, maxStack
- ShopUI มีระบบ tab/category ดีมาก
- Freeze player controls ขณะเปิด UI (ปิด ThirdPersonController, Cinemachine)
- Shop มี SFX สำหรับ buy success/fail

### สิ่งที่ควรปรับปรุง

**[ปานกลาง] Shop ซื้อแล้วยัดเข้า Inventory เท่านั้น**
```csharp
bool added = InventoryUI.Instance.AddItemToInventory(item, amount);
```
ไม่ได้ลองใส่ Hotbar ก่อน ถ้า Inventory เต็มแต่ Hotbar ยังว่าง จะซื้อไม่ได้

**[ปานกลาง] ไม่มี buy price multiplier ตามฤดูกาล/supply-demand**
ตามเอกสาร concept ต้องการระบบ Demand-Supply แต่ราคาตอนนี้เป็น fixed

**[เล็กน้อย] PlayerWallet ไม่ destroy duplicate**
```csharp
void Awake() { Instance = this; } // ไม่มี check duplicate
```
ต่างจาก GameManager ที่มี check ควรเพิ่ม null check เหมือน singleton ตัวอื่น

---

## 6. NPC & Dialogue

**ไฟล์:** `DialogueManager.cs`, `NPCInteractable.cs`, `DialogueSO.cs`

### จุดเด่น
- Typewriter effect สมบูรณ์ กดซ้ำแสดงทั้งบรรทัดเลย
- NPCInteractable มี Gizmo ช่วยดู interact radius ใน Editor
- NPC หันหน้าหาผู้เล่นเมื่อเริ่มคุย
- DialogueSO ใช้ ScriptableObject ดี ง่ายต่อ content creator

### สิ่งที่ควรปรับปรุง

**[สำคัญ] NPC ไม่มีระบบ quest**
ตาม concept ต้องรับ quest จาก NPC แต่ตอนนี้มีแค่ dialogue ยังไม่มี quest system

**[ปานกลาง] NPCInteractable ไม่ null-check DialogueManager**
```csharp
bool isTalking = DialogueManager.Instance.IsDialogueActive; // อาจ null
```
ถ้า DialogueManager ยังไม่ถูกสร้างจะ crash

**[ปานกลาง] NPC ยังไม่มีพฤติกรรม (AI)**
ตาม concept: NPC เดินรอบเมือง, มีวันเกิด, มีความสัมพันธ์ แต่ตอนนี้ NPC ยืนนิ่งรอคุยอย่างเดียว

**[เล็กน้อย] DialogueManager ใช้ StopAllCoroutines()**
อาจกระทบ coroutine อื่นถ้ามี ควรเก็บ reference แล้ว StopCoroutine เฉพาะตัว

---

## 7. ItemSO (ระบบไอเท็ม)

**ไฟล์:** `ItemSO.cs`

### จุดเด่น
- ออกแบบดี มี category (Tool, Seed, Consumable), toolAction, energyCost, sellPrice, stackable

### สิ่งที่ควรปรับปรุง

**[ปานกลาง] ItemCategory ยังขาดหลาย type**
```csharp
public enum ItemCategory { Tool, Seed, Consumable }
```
ในอนาคตต้องเพิ่ม: Ore, Fish, Furniture, Equipment, QuestItem ฯลฯ

**[ปานกลาง] ToolAction ยังขาด**
```csharp
public enum ToolAction { None, Hoe, Water, Axe }
```
ต้องเพิ่ม: FishingRod, Pickaxe, Hammer ฯลฯ

**[เล็กน้อย] ไม่มี buyPrice แยกจาก sellPrice**
ตอนนี้ราคาซื้อกำหนดใน ShopDefinition แยกจาก ItemSO ซึ่ง OK แต่อาจจะสับสนถ้ามี item เยอะ

---

## สรุป Priority ที่ควรแก้ก่อน

### ด่วน (ควรแก้ก่อนเพิ่มระบบใหม่)
1. **Save ไม่เก็บเงินและหนี้** - ข้อมูลหายเมื่อ load
2. **Dialogue check หลัง movement** - ผู้เล่นเดินได้ขณะคุย NPC  
3. **Crop ใช้ real-time แทน game-time** - พืชโตไม่ sync กับเวลาในเกม

### ปานกลาง (แก้เมื่อจะเพิ่มระบบใหม่)
4. OnDateChanged fire ทุกเฟรม - อาจช้าเมื่อมี listener เยอะ
5. SellBox ขายหมดไม่มี confirm
6. PlayerWallet singleton ไม่ check duplicate
7. FindObjectsOfType ในระบบ save

### เตรียมพร้อมสำหรับระบบใหม่
8. เพิ่ม Season enum ใน CalendarSystem
9. ขยาย ItemCategory และ ToolAction
10. เพิ่ม Quest system structure
11. เพิ่ม NPC AI / behavior system

---

## ระบบที่พร้อมต่อยอด

| ระบบใหม่ | ต่อยอดจาก | ความยากโดยประมาณ |
|----------|-----------|-----------------|
| ตกปลา | PlayerController.StartFishing(), ItemSO | ปานกลาง |
| ขุดเหมือง | FarmingSystem pattern, ItemSO | ยาก (ต้องมี combat) |
| ฤดูกาล | CalendarSystem | ง่าย |
| เปิร์ค | PlayerEnergy, FarmingSystem | ปานกลาง |
| ระบบลงโทษ | MonthlyDebtManager | ง่าย-ปานกลาง |
| Quest | NPCInteractable, DialogueSO | ปานกลาง |
| อัพเกรด | ItemSO, PlayerWallet | ปานกลาง |
| อุปสรรค | CalendarSystem (ฤดูกาล), SoilTile | ปานกลาง |
