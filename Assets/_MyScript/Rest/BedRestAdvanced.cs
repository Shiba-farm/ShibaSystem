using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Collider))]
public class BedRestAdvanced : MonoBehaviour
{
    [Header("Interact")]
    public KeyCode interactKey = KeyCode.E;
    public float interactRadius = 2.0f;
    public Transform player;

    [Header("Prompt")]
    public CanvasGroup promptPanel;         // "Press E to rest"
    public TextMeshProUGUI promptText;
    [Tooltip("ถ้าต้องการแยก Text สำหรับแสดงคูลดาวน์ ให้ใส่ตัวนี้ (ไม่ใส่ก็ได้)")]
    public TextMeshProUGUI cooldownText;

    [Header("Sleep UI (เลือกเวลาตื่น)")]
    public CanvasGroup sleepPanel;          // Panel UI เลือกเวลา
    public Slider hourSlider;               // 0..23
    public Slider minuteSlider;             // 0..59
    public int minuteStep = 5;              // ขยับทีละกี่นาที (1 = ทุกนาที)
    public TextMeshProUGUI timePreview;     // แสดงผล hh:mm
    public Button confirmButton;
    public Button cancelButton;

    [Header("Fade")]
    public CanvasGroup fadeCanvas;          // จอดำ
    public float fadeDuration = 0.35f;
    public float sleepSeconds = 1.5f;

    [Header("Energy")]
    public PlayerEnergy energy;
    public bool fillToMax = true;
    public int restoreAmount = 50;

    [Header("Cooldown")]
    public float restCooldown = 3f;
    private float lastRestTime = -999f;

    [Header("หยุดคอนโทรลขณะเปิด UI")]
    public MonoBehaviour[] disableWhileUI;

    bool inRange;
    bool uiOpen;

    // ===== Helpers =====
    float CooldownRemaining => Mathf.Max(0f, restCooldown - (Time.time - lastRestTime));
    bool IsOnCooldown => CooldownRemaining > 0.01f;

    void Start()
    {
        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (confirmButton) confirmButton.onClick.AddListener(OnConfirmSleep);
        if (cancelButton) cancelButton.onClick.AddListener(OnCancelSleep);

        SetCG(promptPanel, 0, false);
        SetCG(sleepPanel, 0, false);
        SetCG(fadeCanvas, 0, false);
        if (cooldownText) cooldownText.gameObject.SetActive(false);

        if (hourSlider)
        {
            hourSlider.wholeNumbers = true;
            hourSlider.minValue = 0; hourSlider.maxValue = 23;
        }
        if (minuteSlider)
        {
            minuteSlider.wholeNumbers = true;
            minuteSlider.minValue = 0; minuteSlider.maxValue = 59;
            minuteSlider.onValueChanged.AddListener(v =>
            {
                int step = Mathf.Max(1, minuteStep);
                int snapped = Mathf.RoundToInt(v / step) * step;
                snapped = Mathf.Clamp(snapped, (int)minuteSlider.minValue, (int)minuteSlider.maxValue);
                if ((int)v != snapped) minuteSlider.SetValueWithoutNotify(snapped);
                UpdateTimePreview();
            });
        }
        UpdateTimePreview();
    }

    void Update()
    {
        if (player)
            inRange = Vector3.Distance(player.position, transform.position) <= interactRadius;

        // Prompt + Cooldown display
        if (promptPanel)
        {
            bool showPrompt = inRange && !uiOpen;
            SetCG(promptPanel, showPrompt ? 1f : 0f, showPrompt);

            if (showPrompt)
            {
                if (IsOnCooldown)
                {
                    string cd = $"{CooldownRemaining:0.0}s";
                    if (cooldownText)            // มี Text แยก
                    {
                        cooldownText.gameObject.SetActive(true);
                        cooldownText.text = cd;
                        if (promptText) promptText.text = "Rest cooldown";
                    }
                    else                         // ไม่มี Text แยก -> โชว์บน prompt
                    {
                        if (promptText) promptText.text = $"Rest cooldown ({cd})";
                    }
                }
                else
                {
                    if (cooldownText) cooldownText.gameObject.SetActive(false);
                    if (promptText) promptText.text = $"Press {interactKey} to rest.";
                }
            }
            else
            {
                if (cooldownText) cooldownText.gameObject.SetActive(false);
            }
        }

        // เปิด UI
        if (inRange && !uiOpen && Input.GetKeyDown(interactKey))
        {
            if (IsOnCooldown) return;
            OpenSleepUI();
        }

        if (uiOpen) UpdateTimePreview();
    }

    void OpenSleepUI()
    {
        uiOpen = true;

        int defaultHour = 6, defaultMinute = 0;
        var t = TimeOfDaySystem.Instance;
        if (t) { defaultHour = 6; defaultMinute = 0; }

        if (hourSlider) hourSlider.value = defaultHour;
        if (minuteSlider) minuteSlider.value = defaultMinute;

        SetCG(sleepPanel, 1, true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SetControlsEnabled(false);
    }

    void OnCancelSleep()
    {
        CloseSleepUI();
        SetControlsEnabled(true);
    }

    void CloseSleepUI()
    {
        uiOpen = false;
        SetCG(sleepPanel, 0, false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void OnConfirmSleep()
    {
        if (!hourSlider || !minuteSlider) return;
        int targetHour = (int)hourSlider.value;
        int targetMinute = (int)minuteSlider.value;

        CloseSleepUI();
        StartCoroutine(SleepRoutine(targetHour, targetMinute));
    }

    IEnumerator SleepRoutine(int targetHour, int targetMinute)
    {
        lastRestTime = Time.time;

        yield return Fade(1f, fadeDuration);
        yield return new WaitForSeconds(sleepSeconds);

        if (energy)
        {
            if (fillToMax) energy.RefillEnergy(energy.maxEnergy);
            else energy.RefillEnergy(restoreAmount);
        }

        var t = TimeOfDaySystem.Instance;
        if (t)
        {
            int nowH = t.Hour, nowM = t.Minute;
            bool nextDay = (targetHour < nowH) || (targetHour == nowH && targetMinute <= nowM);
            t.SetTime(targetHour, targetMinute);
        }

        yield return Fade(0f, fadeDuration);
        SetControlsEnabled(true);
    }

    IEnumerator Fade(float to, float duration)
    {
        if (!fadeCanvas) yield break;
        float from = fadeCanvas.alpha;
        float t = 0f;
        SetCG(fadeCanvas, fadeCanvas.alpha, true);

        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            fadeCanvas.alpha = a;
            yield return null;
        }
        SetCG(fadeCanvas, to, to > 0f);
    }

    void SetCG(CanvasGroup cg, float alpha, bool interactable)
    {
        if (!cg) return;
        cg.alpha = alpha;
        cg.blocksRaycasts = interactable;
        cg.interactable = interactable;
    }

    void UpdateTimePreview()
    {
        if (!timePreview) return;
        int h = hourSlider ? (int)hourSlider.value : 0;
        int m = minuteSlider ? (int)minuteSlider.value : 0;
        timePreview.text = $"{h:00}:{m:00}";
    }

    void SetControlsEnabled(bool enabled)
    {
        if (disableWhileUI == null) return;
        foreach (var comp in disableWhileUI)
        {
            if (!comp) continue;
            comp.enabled = enabled;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
#endif
}
