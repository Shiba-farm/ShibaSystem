using UnityEngine;

public class HotbarUIController : InventoryMainUIs, ILocalSaveable
{
    [SerializeField] private HeldItemSignal heldItemSignal;

    private int   _selectedIndex = 0;
    private int   _lastNumEnter  = 1;
    private ulong _ownerClientId;
    public  ulong OwnerClientId => _ownerClientId;

    public bool IsPlayerSaveable => true;

    // ── Setup ────────────────────────────────────────────────────────────────
    public void SetOwnerClientId(ulong clientId)
    {
        _ownerClientId = clientId;
        TryRegister();
    }

    private void TryRegister()
    {
        if (SaveLoadManager.Instance != null)
            SaveLoadManager.Instance.Register(this);
    }

    // ── Enable / Disable ─────────────────────────────────────────────────────
    protected override void OnEnable()
    {
        base.OnEnable();
        InputHandler.Singleton.OnNumkeyTriggered      += SelectSlotByKeyboard;
        InputHandler.Singleton.OnHotbarScrollTriggered += SelectSlotByScroll;
        TryRegister();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        InputHandler.Singleton.OnNumkeyTriggered      -= SelectSlotByKeyboard;
        InputHandler.Singleton.OnHotbarScrollTriggered -= SelectSlotByScroll;
        SaveLoadManager.Instance?.Unregister(this);
    }

    // ── Post-populate init ────────────────────────────────────────────────────
    /// <summary>เรียกหลัง PopulateSlots ทุกครั้ง — restore การ highlight slot ที่เลือกอยู่</summary>
    protected override void OnSlotsPopulated()
    {
        // clamp เผื่อ slot จำนวนน้อยลงหลัง rebuild
        if (allSlots == null || allSlots.Length == 0) return;
        _selectedIndex = Mathf.Clamp(_selectedIndex, 0, allSlots.Length - 1);
        allSlots[_selectedIndex].SetSelected(true);
        heldItemSignal?.Set(allSlots[_selectedIndex].currentItem, _selectedIndex);
    }

    /// <summary>
    /// เรียกทุกครั้งที่ข้อมูลช่อง hotbar เปลี่ยน (ไม่ใช่แค่ตอนสร้างใหม่ทั้งหมด) — เช่น
    /// ไอเทมในช่องที่ "ถืออยู่" ถูกใช้ไปจนหมด หรือมีคนลากไอเทมใหม่มาทับช่องเดิมที่เลือกอยู่
    ///
    /// บั๊กที่เจอ: ถ้าไม่ override ตัวนี้ HeldItemSignal จะอัปเดตเฉพาะตอนกดเลข/scroll เปลี่ยน slot
    /// เท่านั้น — ถ้าของในช่องที่ถืออยู่เปลี่ยนไปเองโดยไม่ได้เปลี่ยน slot (เช่น ให้ของขวัญจนของหมด
    /// แล้วลากไอเทมใหม่มาใส่ช่องเดิม) HeldItemSignal.Current จะค้างเป็นไอเทมเก่า (stale) ทำให้
    /// ระบบอื่น (เช่น ให้ของขวัญ NPC) เข้าใจผิดว่ายังถือไอเทมเก่าอยู่ทั้งๆ ที่ UI โชว์ไอเทมใหม่แล้ว
    /// </summary>
    public override void RefreshAllSlots()
    {
        base.RefreshAllSlots();

        if (allSlots == null || allSlots.Length == 0 || heldItemSignal == null) return;

        int idx = Mathf.Clamp(_selectedIndex, 0, allSlots.Length - 1);
        ItemSO current = allSlots[idx].currentItem;

        // เช็คก่อนว่าต่างจริงไหม กัน re-invoke OnChanged ซ้ำๆ ทุกครั้งที่มีอะไรก็ตามในกระเป๋าเปลี่ยน
        // (ไม่งั้นของที่ถืออยู่ในมือจะสั่น/สร้างใหม่ทุกครั้งโดยไม่จำเป็น)
        if (heldItemSignal.Current != current)
            heldItemSignal.Set(current, idx);
    }

    // ── Selection ─────────────────────────────────────────────────────────────

    /// <summary>เลือก slot ด้วย real index (0-based) — ใช้ภายใน</summary>
    private void SelectSlotByRealIndex(int realIndex)
    {
        if (allSlots == null || allSlots.Length == 0) return;
        if (realIndex < 0 || realIndex >= allSlots.Length) return;

        allSlots[_selectedIndex].SetSelected(false);
        allSlots[realIndex].SetSelected(true);
        _selectedIndex = realIndex;

        heldItemSignal.Set(allSlots[realIndex].currentItem, realIndex);
    }

    /// <summary>เลือก slot ด้วยปุ่มตัวเลข 1-9 (1-based)</summary>
    private void SelectSlotByKeyboard(int keyIndex)
    {
        int realIndex = keyIndex - 1;
        if (realIndex < 0 || realIndex >= (allSlots?.Length ?? 0)) return;
        _lastNumEnter = keyIndex;
        SelectSlotByRealIndex(realIndex);
    }

    /// <summary>
    /// เลือก slot ด้วย scroll wheel — direction: +1 = ถัดไป, -1 = ก่อนหน้า
    /// วนรอบเมื่อถึงปลาย (wrap-around)
    /// </summary>
    private void SelectSlotByScroll(int direction)
    {
        if (allSlots == null || allSlots.Length == 0) return;

        int next = (_selectedIndex + direction + allSlots.Length) % allSlots.Length;
        _lastNumEnter = next + 1; // sync กับ 1-based เผื่อ save
        SelectSlotByRealIndex(next);
    }

    // ── Save / Load ───────────────────────────────────────────────────────────
    public void CaptureState(GameSaveData save, ulong clientId = 0)
    {
        var playerData = save.GetOrCreatePlayer(clientId);
        playerData.heldSlotIndex = _lastNumEnter;
    }

    public void RestoreState(GameSaveData save, ulong clientId = 0)
    {
        var playerData = save.FindPlayer(clientId);
        if (playerData == null) return;
        SelectSlotByRealIndex(playerData.heldSlotIndex);
    }
}
