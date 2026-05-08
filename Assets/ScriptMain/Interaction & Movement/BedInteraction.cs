using UnityEngine;
using TMPro;

public class BedInteraction : MonoBehaviour, IInteractable
{
    [Header("Signal")]
    [SerializeField] private WorldTimeSignal timeSignal;

    [Header("Time Restriction (optional)")]
    [Tooltip("ถ้า true จะนอนได้เฉพาะหลังเวลาที่กำหนด (earlyBedtimeHour)")]
    public bool restrictBedtime = false;
    [Tooltip("นอนได้หลัง กี่โมง เช่น 20 = 20:00")]
    [Range(0, 23)] public int earlyBedtimeHour = 20;

    public PromptType InteractPromptType => PromptType.Bed;

    public void Interact()
    {
        // ตรวจสอบเวลา (ถ้าเปิด restrict)
        // if (restrictBedtime && TimeOfDaySystem.Instance != null)
        // {
        //     if (timeSignal.CurrentTime.Hour < earlyBedtimeHour)
        //     {
        //         Debug.Log($"[Bed] ยังนอนไม่ได้ — ต้องรอถึง {earlyBedtimeHour}:00");
        //         return;
        //     }
        // }

        // // ตรวจสอบว่า DayEndSystem พร้อมหรือยัง
        // if (DayEndSystem.Instance == null)
        // {
        //     Debug.LogWarning("[Bed] ไม่พบ DayEndSystem ใน Scene!");
        //     return;
        // }

        // DayEndSystem.Instance.ForceSleep();
        Debug.Log("Sleep");
        InGameUIManager.Instance.OpenExclusivePanel("Summary");
    }
}
