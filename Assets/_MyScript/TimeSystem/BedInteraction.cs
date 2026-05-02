using UnityEngine;
using TMPro;

/// <summary>
/// ติดไว้กับ GameObject เตียง
/// — Player เดินเข้าใกล้ → Prompt ขึ้น
/// — กด E → DayEndSystem.ForceSleep() → แสดง Summary → เปลี่ยนวัน
///
/// Setup:
///   1. ติด Script นี้กับ Bed GameObject
///   2. ใส่ Collider (Is Trigger = true) ให้เตียง
///   3. กำหนด promptPanel / promptLabel ใน Inspector (optional)
///   4. ระบบจะใช้ DayEndSystem.Instance.ForceSleep() โดยอัตโนมัติ
/// </summary>
public class BedInteraction : MonoBehaviour
{
    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Prompt UI (optional)")]
    [Tooltip("Panel ที่แสดง prompt — ปล่อยว่างได้ถ้าใช้ WorldSpace หรือไม่มี UI")]
    public GameObject promptPanel;
    public TextMeshProUGUI promptLabel;
    public string promptMessage = "กด [E] เพื่อนอนหลับ";

    [Header("Time Restriction (optional)")]
    [Tooltip("ถ้า true จะนอนได้เฉพาะหลังเวลาที่กำหนด (earlyBedtimeHour)")]
    public bool restrictBedtime = false;
    [Tooltip("นอนได้หลัง กี่โมง เช่น 20 = 20:00")]
    [Range(0, 23)] public int earlyBedtimeHour = 20;

    // ─── Runtime ──────────────────────────────────────────────────────
    bool _playerInRange;

    // ──────────────────────────────────────────────────────────────────
    void Start()
    {
        HidePrompt();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;
        ShowPrompt();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        HidePrompt();
    }

    void Update()
    {
        if (!_playerInRange) return;
        if (!Input.GetKeyDown(interactKey)) return;

        TrySleep();
    }

    // ─── Logic ────────────────────────────────────────────────────────

    void TrySleep()
    {
        // ตรวจสอบเวลา (ถ้าเปิด restrict)
        if (restrictBedtime && TimeOfDaySystem.Instance != null)
        {
            if (TimeOfDaySystem.Instance.Hour < earlyBedtimeHour)
            {
                Debug.Log($"[Bed] ยังนอนไม่ได้ — ต้องรอถึง {earlyBedtimeHour}:00");
                // เปลี่ยน prompt ชั่วคราว (optional)
                if (promptLabel)
                    promptLabel.text = $"นอนได้หลัง {earlyBedtimeHour}:00 น.";
                return;
            }
        }

        // ตรวจสอบว่า DayEndSystem พร้อมหรือยัง
        if (DayEndSystem.Instance == null)
        {
            Debug.LogWarning("[Bed] ไม่พบ DayEndSystem ใน Scene!");
            return;
        }

        HidePrompt();
        _playerInRange = false;

        DayEndSystem.Instance.ForceSleep();
    }

    // ─── UI Helpers ───────────────────────────────────────────────────

    void ShowPrompt()
    {
        if (promptLabel) promptLabel.text = promptMessage;
        if (promptPanel) promptPanel.SetActive(true);
    }

    void HidePrompt()
    {
        if (promptPanel) promptPanel.SetActive(false);
    }
}
