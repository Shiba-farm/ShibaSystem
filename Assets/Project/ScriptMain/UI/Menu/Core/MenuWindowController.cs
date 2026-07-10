using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Window Controller — เป็นตัวแทนของ "หน้าต่างเมนูรวม" ทั้งบาน (กรอบใหญ่ที่มี
/// แท็บ 1-6 + เงิน + ปุ่ม X ตาม mockup) ไม่ตัดสินใจว่า "เปิด/ปิด panel ยังไง"
/// (เรื่องนั้นเป็นของ InGameUIManager ที่มีอยู่แล้ว — ใช้ fade canvas group เดิม)
/// แต่เป็นจุดเดียวที่ระบบอื่นในเกม (NPC, Quest giver, ปุ่ม UI อื่น ๆ) เรียกใช้เพื่อ
/// "เปิดเมนูไปที่แท็บที่ต้องการ" — ทำหน้าที่เป็น Facade ลดการ coupling
///
/// ตัวอย่างการเรียกจากระบบอื่น:
///   MenuWindowController.Instance.OpenToTab(MenuTabId.Relationships);
/// </summary>
public class MenuWindowController : MonoBehaviour, IInitializableUI
{
    public static MenuWindowController Instance { get; private set; }

    [SerializeField] private MenuTabController tabController;
    [SerializeField] private Button closeButton;

    private bool _initialized;

    private void Awake()
    {
        // Window อาจถูก SetActive(false) ตอน start (InGameUIManager ปิด panel ทั้งหมด
        // ใน InitializePanels) — ใช้ Awake ไม่ใช่ OnEnable เพื่อให้ Instance พร้อมใช้เสมอ
        Instance = this;
    }

    public void InitializeUI()
    {
        if (_initialized) return;
        _initialized = true;

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        tabController?.ShowDefaultOrLastTab();
    }

    /// <summary>เปิดหน้าต่างเมนู (ถ้ายังไม่เปิด) แล้วสลับไปที่แท็บที่ระบุ</summary>
    public void OpenToTab(MenuTabId tab)
    {
        if (InGameUIManager.Instance != null)
        {
            if (!gameObject.activeSelf)
                InGameUIManager.Instance.OpenExclusivePanel(InGamePanel.Menu);
        }
        else
        {
            gameObject.SetActive(true);
        }

        tabController?.ShowTab(tab);
    }

    public void Close()
    {
        if (InGameUIManager.Instance != null)
            InGameUIManager.Instance.ClosePanel(InGamePanel.Menu);
        else
            gameObject.SetActive(false);
    }
}
