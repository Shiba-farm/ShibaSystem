/// <summary>
/// Contract ที่ทุกหน้าแท็บ (Inventory / Quest / Relationships / Map / Skills / Achievements)
/// ต้อง implement — ทำให้ MenuTabController คุยกับทุกแท็บด้วย interface เดียว
/// โดยไม่ต้องรู้จัก class จริงของแต่ละแท็บ (Dependency Inversion / SOLID).
///
/// Lifecycle ที่ MenuTabController เรียก:
///   1) InitializeTab()  — เรียกครั้งแรกที่แท็บถูกเปิดเท่านั้น (lazy init, ไม่ใช่ตอน Awake)
///   2) OnTabShown()     — ทุกครั้งที่สลับมาที่แท็บนี้
///   3) OnTabHidden()    — ทุกครั้งที่สลับออกจากแท็บนี้ (สำหรับ pause ตัว listener ที่หนัก)
/// </summary>
public interface IMenuTabView
{
    MenuTabId TabId { get; }
    bool IsInitialized { get; }

    /// <summary>เรียกครั้งเดียวตอนแท็บถูกเปิดเป็นครั้งแรก — ใช้ผูก signal / สร้าง pool</summary>
    void InitializeTab();

    /// <summary>เรียกทุกครั้งที่ผู้เล่นสลับมาแท็บนี้ — ใช้ refresh ข้อมูลล่าสุด</summary>
    void OnTabShown();

    /// <summary>เรียกทุกครั้งที่ผู้เล่นสลับออกจากแท็บนี้</summary>
    void OnTabHidden();
}
