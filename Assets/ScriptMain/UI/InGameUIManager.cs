using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InGameUIManager : MonoBehaviour
{
    public static InGameUIManager Instance { get; private set; }
    [System.Serializable]
    public struct UIPanel
    {
        public string name;
        public CanvasGroup canvasGroup;
        public GameObject panelObject;
    }

    [Header("Panels")]
    [SerializeField] private List<UIPanel> allPanels;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializePanels();
    }

    private void Start()
    {
        if (InputHandler.Singleton != null)
        {
            InputHandler.Singleton.OnPauseTriggered -= HandlePauseToggle;
            InputHandler.Singleton.OnPauseTriggered += HandlePauseToggle;

            InputHandler.Singleton.OnInventoryTriggered -= HandleInventoryToggle;
            InputHandler.Singleton.OnInventoryTriggered += HandleInventoryToggle;
        }
    }

    private void HandlePauseToggle() => OpenExclusivePanel("Pause");
    private void HandleInventoryToggle() => OpenExclusivePanel("Inventory");

    private void InitializePanels()
    {
        foreach (var panel in allPanels)
        {
            IInitializableUI initScript = panel.panelObject.GetComponentInChildren<IInitializableUI>();
            if (initScript != null)
            {
                initScript.InitializeUI();
            }
            if (panel.panelObject != null)
            {
                // if(panel.name == "Crafting") continue; // Crafting panel will be opened by Inventory, so we keep it inactive here
                // 1. Force state to inactive
                panel.panelObject.SetActive(false);

                // 2. Reset CanvasGroup (if assigned) to be safe
                if (panel.canvasGroup != null)
                {
                    panel.canvasGroup.alpha = 0f;
                    panel.canvasGroup.blocksRaycasts = false;
                    panel.canvasGroup.interactable = false;
                }
            }
        }
    }

    private void SetPlayerControl(bool canControl)
    {
        if (InputHandler.Singleton != null)
        {
            InputHandler.Singleton.InputLocked = !canControl;
        }

        Cursor.visible = !canControl;
        Cursor.lockState = canControl ? CursorLockMode.Locked : CursorLockMode.None;
    }

    public void TogglePanel(string panelName)
    {
        foreach (var panel in allPanels)
        {
            if (panel.name == panelName)
            {
                // If the panel is currently hidden, we turn it on first, then fade in
                if (!panel.panelObject.activeSelf)
                {
                    panel.panelObject.SetActive(true);
                    StartCoroutine(FadeCanvas(panel.canvasGroup, 0, 1, 0.25f, null));
                    SetPlayerControl(false);
                }
                // If the panel is currently visible, we fade out first, then turn it off
                else
                {
                    StartCoroutine(FadeCanvas(panel.canvasGroup, 1, 0, 0.25f, () =>
                    {
                        panel.panelObject.SetActive(false);
                        if (!AreAnyPanelsOpen()) SetPlayerControl(true);
                    }));
                }
            }
        }
    }

    public void OpenExclusivePanel(string panelName)
    {
        foreach (var panel in allPanels)
        {
            bool isTarget = panel.name == panelName;

            // CASE 1: This is the panel we want to open
            if (isTarget)
            {
                // If it's already open, we might want to close it (Toggle)
                if (panel.panelObject.activeSelf)
                {
                    StartCoroutine(FadeCanvas(panel.canvasGroup, 1, 0, 0.25f, () =>
                    {
                        panel.panelObject.SetActive(false);
                        if (!AreAnyPanelsOpen()) SetPlayerControl(true);
                    }));
                }
                else
                {
                    panel.panelObject.SetActive(true);
                    StartCoroutine(FadeCanvas(panel.canvasGroup, 0, 1, 0.25f, null));
                    SetPlayerControl(false);
                }
            }
            // CASE 2: This is a different panel that is currently open (Close it!)
            else if (panel.panelObject.activeSelf)
            {
                StartCoroutine(FadeCanvas(panel.canvasGroup, 1, 0, 0.25f, () =>
                {
                    panel.panelObject.SetActive(false);
                }));
            }
        }
    }

    private IEnumerator FadeCanvas(CanvasGroup cg, float start, float end, float duration, System.Action onComplete)
    {
        float elapsed = 0f;
        cg.blocksRaycasts = end > 0;
        cg.interactable = end > 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        cg.alpha = end;

        onComplete?.Invoke();
    }

    private bool AreAnyPanelsOpen()
    {
        foreach (var p in allPanels)
        {
            if (p.panelObject.activeSelf) return true;
        }
        return false;
    }

    private void OnDestroy()
    {
        if (InputHandler.Singleton != null)
        {
            InputHandler.Singleton.OnPauseTriggered -= HandlePauseToggle;
            InputHandler.Singleton.OnInventoryTriggered -= HandleInventoryToggle;
        }
    }
}
