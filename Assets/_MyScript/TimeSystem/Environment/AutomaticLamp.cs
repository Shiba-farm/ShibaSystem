using UnityEngine;

public class AutomaticLamp : MonoBehaviour
{
    [Header("Settings")]
    public Light lampLight; // ลาก Point Light มาใส่ตรงนี้

    [Header("Visuals (Optional)")]
    public MeshRenderer lanternRenderer; // ตัวโมเดลโคมไฟ (ถ้าอยากให้เปลี่ยนสีวัสดุ)
    public int materialIndex = 0;        // วัสดุช่องไหนที่จะเปลี่ยน (ปกติช่อง 1 คือส่วนหลอดไฟ)
    public Material lightOnMat;          // วัสดุตอนไฟติด (Emission)
    public Material lightOffMat;         // วัสดุตอนไฟดับ

    void Start()
    {
        // สมัครรับข้อมูลเวลาจาก TimeOfDaySystem
        var timeSystem = TimeOfDaySystem.Instance;
        if (timeSystem != null)
        {
            timeSystem.OnPhaseChanged += UpdateLampState;

            // เช็คเวลาปัจจุบันทันทีตอนเริ่มเกม
            CheckInitialState(timeSystem);
        }
    }

    void OnDestroy()
    {
        if (TimeOfDaySystem.Instance != null)
        {
            TimeOfDaySystem.Instance.OnPhaseChanged -= UpdateLampState;
        }
    }

    // ฟังก์ชันเช็คตอนเริ่มเกม (คำนวณเองเพราะ GetPhase เป็น private)
    void CheckInitialState(TimeOfDaySystem timeSystem)
    {
        float t = timeSystem.Time01;
        DayPhase phase = DayPhase.Day; // ค่าเริ่มต้น

        if (t >= timeSystem.nightStart || t < timeSystem.dawnStart) phase = DayPhase.Night;
        else if (t >= timeSystem.duskStart) phase = DayPhase.Dusk;
        else if (t >= timeSystem.dayStart) phase = DayPhase.Day;
        else phase = DayPhase.Dawn;

        UpdateLampState(phase);
    }

    void UpdateLampState(DayPhase phase)
    {
        // เงื่อนไข: เปิดไฟเฉพาะตอน "เย็น" หรือ "กลางคืน"
        bool isNightTime = (phase == DayPhase.Dusk || phase == DayPhase.Night);

        // 1. สั่งเปิด/ปิด แสง
        if (lampLight != null)
        {
            lampLight.enabled = isNightTime;
        }

        // 2. (แถม) เปลี่ยนวัสดุให้ดูเรืองแสง
        if (lanternRenderer != null && lightOnMat != null && lightOffMat != null)
        {
            Material[] mats = lanternRenderer.materials;
            if (materialIndex < mats.Length)
            {
                mats[materialIndex] = isNightTime ? lightOnMat : lightOffMat;
                lanternRenderer.materials = mats;
            }
        }
    }
}