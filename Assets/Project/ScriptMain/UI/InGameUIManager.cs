using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ── Panel Enum ───────────────────────────────────────────────────────────────
// เพิ่ม panel ใหม่ที่นี่เพียงที่เดียว — ไม่ต้องกลัว typo อีกต่อไป
public enum InGamePanel
{
    Pause,
    Inventory,  // เลิกใช้แล้ว — แทนที่ด้วย Menu (หน้าต่างเมนูรวมแบบแท็บ) คงไว้เพื่อไม่ให้ enum index เดิมขยับ
    Summary,
    Debt,
    Crafting,
    Selling,
    Settings,
    Dialogue,
    DayBanner,
    Menu,       // หน้าต่างเมนูรวม (Inventory/Quest/Relationships/Map/Skills/Achievements)
    Fishing,    // mini-game panel — managed via OpenFishingPanel / CloseFishingPanel
    Waiting,    // "Waiting for other players to sleep" panel
}

public class InGameUIManager : MonoBehaviour
{
    public static InGameUIManager Instance { get; private set; }

    [System.Serializable]
    public struct UIPanel
    {
        public InGamePanel panelType;
        public CanvasGroup canvasGroup;
        public GameObject  panelObject;
    }

    [Header("Panels")]
    [SerializeField] private List<UIPanel> allPanels;

    [Header("Fishing Mini-game")]
    [Tooltip("The FishingMiniGameUI script on the Fishing panel. " +
             "InGameUIManager calls Open/ForceClose on it directly.")]
    [SerializeField] private FishingMiniGameUI fishingMiniGamePanel;

    /// <summary>
    /// True while the fishing mini-game (or any future critical panel) is showing.
    /// Blocks Pause, Inventory, and the fishing cancel action while set.
    /// </summary>
    public bool IsCriticalPanelOpen { get; private set; }

    private const float FADE_DURATION = 0.25f;

    // ── Lifecycle ────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        InitializePanels();
    }

    private void Start()
    {
        if (InputHandler.Singleton != null)
        {
            InputHandler.Singleton.OnPauseTriggered     -= HandlePauseToggle;
            InputHandler.Singleton.OnPauseTriggered     += HandlePauseToggle;
            InputHandler.Singleton.OnInventoryTriggered -= HandleInventoryToggle;
            InputHandler.Singleton.OnInventoryTriggered += HandleInventoryToggle;
        }

        // ตั้ง state เริ่มต้นที่แน่นอน — ป้องกัน InputLocked ค้างจาก session ก่อนหน้า (bug #2)
        SetPlayerControl(true);
    }

    private void OnDestroy()
    {
        if (InputHandler.Singleton != null)
        {
            InputHandler.Singleton.OnPauseTriggered     -= HandlePauseToggle;
            InputHandler.Singleton.OnInventoryTriggered -= HandleInventoryToggle;
        }
    }

    // ── Input Handlers ───────────────────────────────────────────────────────
    private void HandlePauseToggle()
    {
        if (IsCriticalPanelOpen) return;
        OpenExclusivePanel(InGamePanel.Pause);
    }

    private void HandleInventoryToggle()
    {
        if (IsCriticalPanelOpen) return;
        OpenExclusivePanel(InGamePanel.Menu);
    }

    // ── Init ─────────────────────────────────────────────────────────────────
    private void InitializePanels()
    {
        foreach (var panel in allPanels)
        {
            if (panel.panelObject == null) continue;

            var initScript = panel.panelObject.GetComponentInChildren<IInitializableUI>();
            initScript?.InitializeUI();

            panel.panelObject.SetActive(false);

            if (panel.canvasGroup != null)
            {
                panel.canvasGroup.alpha          = 0f;
                panel.canvasGroup.blocksRaycasts = false;
                panel.canvasGroup.interactable   = false;
            }
        }
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>เปิด/ปิด panel โดยไม่ปิด panel อื่น</summary>
    public void TogglePanel(InGamePanel panelType)
    {
        int idx = FindPanelIndex(panelType);
        if (idx < 0) { LogMissing(panelType); return; }

        var panel = allPanels[idx];
        if (!panel.panelObject.activeSelf)
        {
            panel.panelObject.SetActive(true);
            StartCoroutine(FadeCanvas(panel.canvasGroup, 0f, 1f, FADE_DURATION, null));
            SetPlayerControl(false);
        }
        else
        {
            StartCoroutine(FadeCanvas(panel.canvasGroup, 1f, 0f, FADE_DURATION, () =>
            {
                panel.panelObject.SetActive(false);
                if (!AreAnyPanelsOpen()) SetPlayerControl(true);
            }));
        }
    }

    /// <summary>เปิด panel นี้และปิด panel อื่นทั้งหมด (toggle ถ้าเปิดอยู่แล้ว)</summary>
    public void OpenExclusivePanel(InGamePanel panelType)
    {
        int targetIdx = FindPanelIndex(panelType);
        if (targetIdx < 0) { LogMissing(panelType); return; }

        for (int i = 0; i < allPanels.Count; i++)
        {
            var  panel    = allPanels[i];
            bool isTarget = i == targetIdx;

            if (isTarget)
            {
                if (panel.panelObject.activeSelf)
                {
                    // Toggle off
                    StartCoroutine(FadeCanvas(panel.canvasGroup, 1f, 0f, FADE_DURATION, () =>
                    {
                        panel.panelObject.SetActive(false);
                        if (!AreAnyPanelsOpen()) SetPlayerControl(true);
                    }));
                }
                else
                {
                    panel.panelObject.SetActive(true);
                    StartCoroutine(FadeCanvas(panel.canvasGroup, 0f, 1f, FADE_DURATION, null));
                    SetPlayerControl(false);
                }
            }
            else if (panel.panelObject.activeSelf)
            {
                // ปิด panel อื่นที่เปิดอยู่
                StartCoroutine(FadeCanvas(panel.canvasGroup, 1f, 0f, FADE_DURATION, () =>
                    panel.panelObject.SetActive(false)));
            }
        }
    }

    /// <summary>ปิด panel ที่ระบุทันที ไม่มี fade</summary>
    public void ClosePanel(InGamePanel panelType)
    {
        int idx = FindPanelIndex(panelType);
        if (idx < 0) return;

        var panel = allPanels[idx];
        if (panel.canvasGroup != null)
        {
            panel.canvasGroup.alpha          = 0f;
            panel.canvasGroup.blocksRaycasts = false;
            panel.canvasGroup.interactable   = false;
        }
        panel.panelObject.SetActive(false);
        if (!AreAnyPanelsOpen()) SetPlayerControl(true);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private int FindPanelIndex(InGamePanel panelType)
    {
        for (int i = 0; i < allPanels.Count; i++)
            if (allPanels[i].panelType == panelType) return i;
        return -1;
    }

    private void SetPlayerControl(bool canControl)
    {
        if (InputHandler.Singleton != null)
            InputHandler.Singleton.InputLocked = !canControl;

        // เกมนี้ใช้ isometric camera — cursor ต้องมองเห็นตลอด (bug #3)
        // ไม่ว่าจะเปิด/ปิด panel ก็ตาม cursor จะแสดงเสมอ
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private bool AreAnyPanelsOpen()
    {
        foreach (var p in allPanels)
            if (p.panelObject != null && p.panelObject.activeSelf) return true;
        return false;
    }

    private IEnumerator FadeCanvas(CanvasGroup cg, float start, float end,
                                   float duration, System.Action onComplete)
    {
        if (cg == null) { onComplete?.Invoke(); yield break; }

        float elapsed         = 0f;
        cg.blocksRaycasts     = end > 0f;
        cg.interactable       = end > 0f;

        while (elapsed < duration)
        {
            elapsed  += Time.deltaTime;
            cg.alpha  = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        cg.alpha = end;
        onComplete?.Invoke();
    }

    private void LogMissing(InGamePanel panelType) =>
        Debug.LogWarning($"[InGameUIManager] Panel '{panelType}' ไม่ได้ assign ใน Inspector");

    // ── Fishing mini-game ─────────────────────────────────────────────────────

    /// <summary>
    /// Opens the Fishing panel and starts the mini-game.
    /// Closes any other open panel first.
    /// Sets IsCriticalPanelOpen so Pause, Inventory, and fishing-cancel are blocked.
    /// </summary>
    public void OpenFishingPanel(float fishMoveSpeed, float uncertainty, float catchTime, float pullStrength)
    {
        if (fishingMiniGamePanel == null)
        {
            Debug.LogWarning("[InGameUIManager] fishingMiniGamePanel is not assigned.");
            return;
        }

        // Close every non-fishing panel that is currently open
        int fishIdx = FindPanelIndex(InGamePanel.Fishing);
        for (int i = 0; i < allPanels.Count; i++)
        {
            if (i == fishIdx) continue;
            var other = allPanels[i];
            if (other.panelObject != null && other.panelObject.activeSelf)
            {
                StartCoroutine(FadeCanvas(other.canvasGroup, 1f, 0f, FADE_DURATION, () =>
                    other.panelObject.SetActive(false)));
            }
        }

        // Show the Fishing panel
        if (fishIdx >= 0)
        {
            var fp = allPanels[fishIdx];
            fp.panelObject.SetActive(true);
            StartCoroutine(FadeCanvas(fp.canvasGroup, 0f, 1f, 0.15f, null));
        }

        IsCriticalPanelOpen = true;
        fishingMiniGamePanel.Open(fishMoveSpeed, uncertainty, catchTime, pullStrength);
    }

    /// <summary>
    /// Closes the Fishing panel and clears the critical-panel flag.
    /// Safe to call even if the panel is already closed.
    /// </summary>
    public void CloseFishingPanel()
    {
        if (fishingMiniGamePanel == null) return;

        int fishIdx = FindPanelIndex(InGamePanel.Fishing);
        if (fishIdx < 0) { IsCriticalPanelOpen = false; return; }

        var fp = allPanels[fishIdx];
        if (!fp.panelObject.activeSelf)
        {
            IsCriticalPanelOpen = false;
            return;
        }

        fishingMiniGamePanel.ForceClose();
        StartCoroutine(FadeCanvas(fp.canvasGroup, 1f, 0f, FADE_DURATION, () =>
        {
            fp.panelObject.SetActive(false);
            IsCriticalPanelOpen = false;
        }));
    }
}
