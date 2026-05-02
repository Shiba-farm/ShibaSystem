# Shop System Upgrade — Setup Guide
# ระบบร้านค้าอัพเกรด — คู่มือตั้งค่า

---

## สรุปสิ่งที่เพิ่ม/แก้ไข

### ไฟล์ใหม่
| ไฟล์ | ที่อยู่ | หน้าที่ |
|------|---------|---------|
| `MarketPriceSystem.cs` | `Economy/Market/` | ระบบ Supply & Demand ราคาตลาด |
| `ShopRestockManager.cs` | `Economy/Shop/` | จัดการ Restock สต๊อกร้านค้าอัตโนมัติ |
| `ItemTooltip.cs` | `Inventory/` | Tooltip แสดงราคาขาย/ข้อมูลเมื่อชี้ไอเท็ม |
| `ItemTooltipTrigger.cs` | `Inventory/` | ใส่บน Slot เพื่อเปิด Tooltip |

### ไฟล์ที่แก้ไข
| ไฟล์ | การเปลี่ยนแปลง |
|------|-----------------|
| `ShopDefinition.cs` | เพิ่ม shopName, merchantName, portrait, restock config, stock per item, DayAvailability |
| `ShopUI.cs` | เขียนใหม่ — รองรับ สต๊อก, feedback text, merchant header, sold out |
| `ShopItemView.cs` | เพิ่ม stockLabel, soldOutOverlay, trendLabel, compat overload |
| `SellBox.cs` | เขียนใหม่ — ใช้ราคาตลาด, RecordSale, feedback, SFX |
| `SaveData.cs` | เพิ่ม marketPrices[], daysSinceRestock |

---

## STEP 1: สร้าง MarketPriceSystem

1. สร้าง **Empty GameObject** ชื่อ `MarketPriceSystem`
2. Add Component → `MarketPriceSystem.cs`
3. ตั้งค่าเริ่มต้น:
   - `minPriceMultiplier` = 0.3 (ราคาลดได้สูงสุด 70%)
   - `maxPriceMultiplier` = 2.0 (ราคาเพิ่มสูงสุด 2 เท่า)
   - `supplyThreshold` = 10 (ขาย 10 ชิ้นก่อนราคาเริ่มลด)
   - `supplyDivisor` = 20 (ยิ่งสูง ราคาลดช้า)
   - `dailyRecoveryRate` = 0.1 (ฟื้นวันละ 10%)

### วิธีทำงาน (Supply & Demand)

```
ขายแตงกวา 5 ชิ้น     → ราคายังปกติ (ต่ำกว่า threshold)
ขายแตงกวา 15 ชิ้น    → ราคาเริ่มลด (oversupply = 5, reduction = 5/20 = 0.25)
                        → ราคา = base × 0.75
ขายแตงกวา 30 ชิ้น    → oversupply = 20, reduction = 20/20 = 1.0
                        → ราคา = base × 0.3 (min)

วันถัดไป ราคาฟื้นตัว 10% → base × 0.37
วันถัดไป                → base × 0.43
...
ฟื้นจน base × 1.0 → reset
```

**กลยุทธ์สำหรับผู้เล่น:** ขายของหลากหลาย ไม่ขายชนิดเดียวซ้ำ ๆ!

---

## STEP 2: สร้าง ShopRestockManager

1. สร้าง **Empty GameObject** ชื่อ `ShopRestockManager`
2. Add Component → `ShopRestockManager.cs`
3. ลาก ShopDefinition SO ทั้งหมดไปใส่ช่อง `All Shops`

---

## STEP 3: อัพเดท ShopDefinition

เปิด ShopDefinition SO ของแต่ละร้านค้า:

### ร้านเมล็ดพันธุ์ (ตัวอย่าง)
- `shopName` = "ร้านเมล็ดพันธุ์ซากุระ"
- `merchantName` = "คุณซากุระ"
- `merchantPortrait` = (ลาก sprite portrait)
- `restockIntervalDays` = 3 (เติมของทุก 3 วัน)
- Items:
  - เมล็ดแตงกวา: price=50, maxPerClick=10, **maxStock=20**, category=Seeds
  - เมล็ดมะเขือ: price=80, maxPerClick=10, **maxStock=15**, category=Seeds
  - เมล็ดข้าวโพด: price=120, maxPerClick=5, **maxStock=10**, category=Seeds

### ร้านเครื่องมือ (ตัวอย่าง)
- `shopName` = "ร้านช่างเหล็กทานูกิ"
- `merchantName` = "คุณฮัมเมอร์"
- `restockIntervalDays` = 7 (เติมทุก 7 วัน)
- Items:
  - จอบ: price=500, maxStock=-1 (ไม่จำกัด), category=Tools
  - บัวรดน้ำ: price=300, maxStock=-1, category=Tools
  - เหล็ก: price=100, **maxStock=30**, category=Materials
  - ไม้: price=50, **maxStock=50**, category=Materials

### ร้านอาหาร (ตัวอย่าง)
- `shopName` = "ร้านอาหารแม่มิโซะ"
- `merchantName` = "แม่มิโซะ"
- `restockIntervalDays` = 1 (เติมทุกวัน)
- Items:
  - ขนมปัง: price=30, maxStock=10, category=Food
  - ข้าวปั้น: price=50, maxStock=5, category=Food
  - ชาเขียว: price=20, maxStock=15, category=Food

---

## STEP 4: อัพเดท ShopItemView Prefab

เพิ่ม element ใน ShopItemView Prefab:

```
ShopItemView (เดิม)
├── Icon (Image)
├── NameLabel (TMP)
├── PriceLabel (TMP)
├── AmountInput (TMP_InputField)
├── MinusBtn (Button)
├── PlusBtn (Button)
├── BuyBtn (Button)
├── [NEW] StockLabel (TMP) ← ลากไปใส่ stockLabel
├── [NEW] SoldOutOverlay (Panel สีดำ semi-transparent) ← ลากไปใส่ soldOutOverlay
│   └── Text "ของหมดแล้ว"
└── [NEW] TrendLabel (TMP) ← ลากไปใส่ trendLabel (optional)
```

---

## STEP 5: อัพเดท ShopUI

เพิ่ม field ในตัว ShopUI:

```
ShopUI (Inspector)
├── (เดิมทั้งหมด)
├── [NEW] merchantNameLabel → ลาก TMP ที่แสดงชื่อ NPC
├── [NEW] merchantPortraitImage → ลาก Image ที่แสดงหน้า NPC
└── [NEW] feedbackText → ลาก TMP ด้านล่างสำหรับแสดง "ซื้อ xx สำเร็จ"
```

---

## STEP 6: ตั้งค่า Item Tooltip

### สร้าง Tooltip Panel ใน Canvas:

```
Canvas_Inventory (หรือ Canvas เดียวกับ Inventory)
├── TooltipPanel (Panel) ← ลากไปใส่ ItemTooltip.tooltipPanel
│   ├── ItemIcon (Image) ← itemIcon
│   ├── ItemName (TMP) ← itemNameText (Bold, สีทอง)
│   ├── CategoryText (TMP) ← categoryText (สีเทา, เล็กลง)
│   ├── SellPriceText (TMP) ← sellPriceText
│   ├── MarketTrendText (TMP) ← marketTrendText
│   └── DescriptionText (TMP) ← descriptionText
```

### ตั้งค่า:
1. สร้าง Empty GameObject → Add `ItemTooltip.cs`
2. ลาก UI elements ตามช่อง
3. ตั้ง `tooltipRect` = RectTransform ของ TooltipPanel
4. TooltipPanel ตั้ง Pivot = (0, 1) (มุมซ้ายบน)

### เพิ่ม Trigger ในแต่ละ Slot:
สำหรับ **InventorySlot** ทุกช่อง:
1. Add Component → `ItemTooltipTrigger.cs`
2. ช่อง `slot` จะหาอัตโนมัติจาก InventorySlot บน object เดียวกัน

---

## STEP 7: อัพเดท SellBox

SellBox จะใช้ราคาตลาดอัตโนมัติถ้ามี `MarketPriceSystem` ในฉาก

เพิ่ม field ใหม่ (Optional):
- `feedbackLabel` → TMP แสดง "ขาย xx — ¥xxx"
- `sfxSource` + `sellSfx` → เสียงเอฟเฟกต์ขาย

---

## STEP 8: อัพเดท Save/Load

ใน GameManager (Save):
```csharp
// === SAVE ===
saveData.marketPrices = MarketPriceSystem.Instance?.GetSaveData();
saveData.daysSinceRestock = ShopRestockManager.Instance?.GetDaysSinceRestock() ?? 0;

// === LOAD ===
if (MarketPriceSystem.Instance != null)
    MarketPriceSystem.Instance.ApplySaveData(saveData.marketPrices);
if (ShopRestockManager.Instance != null)
    ShopRestockManager.Instance.SetDaysSinceRestock(saveData.daysSinceRestock);
```

---

## สรุประบบ Supply & Demand

| สถานการณ์ | ราคาขาย | สี Tooltip |
|-----------|---------|-----------|
| ยังไม่ขายเลย | x1.0 (ราคาปกติ) | ขาว |
| ขาย 10+ ชิ้นเดียวกัน | x0.75 ↓ | แดง |
| ขาย 25+ ชิ้น | x0.3 (ต่ำสุด) | แดง |
| พักขาย 1 วัน | ฟื้น +10% | เหลือง |
| ฟื้นจนราคาปกติ | x1.0 | ขาว |

---

## สรุประบบ Restock

| Config | ผล |
|--------|-----|
| `maxStock = -1` | ไม่จำกัด (เหมือนเดิม) |
| `maxStock = 20` | มี 20 ชิ้น ซื้อหมดแล้ว "ของหมด" |
| `restockIntervalDays = 3` | ทุก 3 วันเติมกลับ 20 ชิ้น |
| `restockIntervalDays = 0` | ไม่มีสต๊อก = ไม่จำกัด |

---

## หลายร้านค้า — NPC แต่ละตัว

NPC แต่ละตัว → ShopTrigger → ShopDefinition คนละ SO

```
NPC_Sakura (ร้านเมล็ด)
├── Collider (Trigger)
└── ShopTrigger
    ├── catalog = SeedShopDefinition
    └── shopUI = ShopUI

NPC_Hammer (ร้านช่างเหล็ก)
├── Collider (Trigger)
└── ShopTrigger
    ├── catalog = ToolShopDefinition
    └── shopUI = ShopUI

NPC_Miso (ร้านอาหาร)
├── Collider (Trigger)
└── ShopTrigger
    ├── catalog = FoodShopDefinition
    └── shopUI = ShopUI
```

แต่ละ ShopDefinition ตั้ง shopName, merchantName, merchantPortrait ต่างกัน
→ ShopUI จะแสดง header + portrait ต่างกันตามร้าน!
