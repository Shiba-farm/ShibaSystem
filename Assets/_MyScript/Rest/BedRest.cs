using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // ใส่ได้ถ้าใช้ TextMeshPro (ไม่ใช้ก็ลบออกได้)

public class BedRest : MonoBehaviour
{
    [Header("Keys")]
    public KeyCode restKey = KeyCode.E;

    [Header("UI Prompt")]
    public GameObject promptPanel;          // ป้าย "Press E to rest."
    public TextMeshProUGUI promptLabel;     // (ไม่บังคับ) จะอัปเดตข้อความให้ตอนคูลดาวน์

    [Header("Sleep FX (optional)")]
    public CanvasGroup fadeCanvas;          // จอดำ (CanvasGroup)
    public float fadeDuration = 0.35f;
    public float sleepSeconds = 1.5f;

    [Header("Energy")]
    public bool fillToMax = true;
    public int restoreAmount = 50;

    [Header("Refs")]
    public PlayerEnergy energy;             // ลาก EnergyBar (ที่มี PlayerEnergy) มาใส่

    [Header("Cooldown")]
    public float restCooldown = 10f;        // เวลาคูลดาวน์ (วินาที) หลังพักเสร็จ
    private float nextRestTime = 0f;        // เวลาที่สามารถพักครั้งถัดไป
    private bool isResting = false;

    private bool playerInRange;

    void Start()
    {
        if (promptPanel) promptPanel.SetActive(false);
        if (energy == null) energy = FindObjectOfType<PlayerEnergy>();
        if (fadeCanvas)
        {
            fadeCanvas.alpha = 0f;
            fadeCanvas.blocksRaycasts = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        if (promptPanel) promptPanel.SetActive(true);
        UpdatePromptText(); // อัปเดตข้อความทันที
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        if (promptPanel) promptPanel.SetActive(false);
    }

    void Update()
    {
        if (!playerInRange) return;

        // แสดงเวลาคูลดาวน์คงเหลือบนป้าย (ถ้ามี label)
        UpdatePromptText();

        // ยังคูลดาวน์อยู่ ห้ามพัก
        if (Time.time < nextRestTime) return;

        // กำลังพักอยู่ ห้ามซ้อน
        if (isResting) return;

        if (Input.GetKeyDown(restKey))
        {
            StartCoroutine(RestRoutine());
        }
    }

    IEnumerator RestRoutine()
    {
        if (energy == null) yield break;

        isResting = true;

        // เฟดเข้า
        if (fadeCanvas) yield return FadeTo(1f, fadeDuration);

        // เติมพลังงาน
        if (fillToMax) energy.RefillEnergy(energy.maxEnergy);
        else energy.RefillEnergy(restoreAmount);

        // เวลานอนพัก
        if (sleepSeconds > 0f) yield return new WaitForSeconds(sleepSeconds);

        // เฟดออก
        if (fadeCanvas) yield return FadeTo(0f, fadeDuration);

        // เริ่มคูลดาวน์
        nextRestTime = Time.time + restCooldown;
        isResting = false;

        // อัปเดตป้ายทันทีหลังพัก
        UpdatePromptText();
    }

    IEnumerator FadeTo(float target, float duration)
    {
        if (!fadeCanvas) yield break;

        float start = fadeCanvas.alpha;
        float t = 0f;
        fadeCanvas.blocksRaycasts = true;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fadeCanvas.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }

        fadeCanvas.alpha = target;
        fadeCanvas.blocksRaycasts = (target > 0.99f);
    }

    void UpdatePromptText()
    {
        if (!promptPanel || !promptLabel) return;

        if (isResting)
        {
            promptLabel.text = "Sleep…";
            return;
        }

        float remain = Mathf.Max(0f, nextRestTime - Time.time);
        if (remain > 0f)
        {
            promptLabel.text = $"You can rest in {remain:0.0}s";
        }
        else
        {
            promptLabel.text = $"Press {restKey} to rest.";
        }
    }
}
