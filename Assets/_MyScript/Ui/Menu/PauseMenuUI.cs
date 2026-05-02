using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Player (optional)")]
    [SerializeField] private Transform player;

    [Header("Behavior")]
    [SerializeField] private bool pauseWithTimeScale = true;
    [SerializeField] private bool unlockCursorOnOpen = true;

    [Tooltip("Optional")]
    [SerializeField] private bool lockCursorWhenPlaying = false;

    [Header("UI Blockers (optional)")]
    [SerializeField] private List<GameObject> uiBlockers = new List<GameObject>();

    [Header("Freeze Components (optional)")]
    [SerializeField] private List<Behaviour> extraDisable = new List<Behaviour>();

    private readonly List<Behaviour> _autoDisable = new List<Behaviour>();
    private bool _isPaused;

    private bool AnyBlockerOpen()
    {
        for (int i = 0; i < uiBlockers.Count; i++)
        {
            var go = uiBlockers[i];
            if (go != null && go.activeInHierarchy) return true;
        }
        return false;
    }

    private void Awake()
    {
        BuildDisableList();
        SetPanels(false, false);
        ApplyPause(false);
    }

    private void BuildDisableList()
    {
        _autoDisable.Clear();

        if (player == null) return;

        var behaviours = player.GetComponentsInChildren<Behaviour>(true);
        foreach (var b in behaviours)
        {
            if (b == null) continue;

            if (b == this) continue;

            if (b.GetType().Name == "InventoryUI") continue;
            if (b.GetType().Name == "PauseMenuUI") continue;
        }
    }

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (InventoryMainUI.IsOpen) return;

            if (_isPaused && settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            else
            {
                TogglePause();
            }
        }
    }

    private void LateUpdate()
    {
        ApplyCursorState();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) ApplyCursorState();
    }

    public void TogglePause()
    {
        if (_isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (AnyBlockerOpen()) return;

        _isPaused = true;
        SetPanels(true, false);
        ApplyPause(true);
    }

    public void Resume()
    {
        _isPaused = false;
        SetPanels(false, false);
        ApplyPause(false);
    }

    public void OpenSettings()
    {
        if (!_isPaused) Pause();
        SetPanels(false, true);
        ApplyPause(true);
    }

    public void CloseSettings()
    {
        if (!_isPaused) return;
        SetPanels(true, false);
        ApplyPause(true);
    }

    private void SetPanels(bool showPause, bool showSettings)
    {
        if (pausePanel != null) pausePanel.SetActive(showPause);
        if (settingsPanel != null) settingsPanel.SetActive(showSettings);
    }

    private void ApplyPause(bool paused)
    {
        if (pauseWithTimeScale)
            Time.timeScale = paused ? 0f : 1f;
        for (int i = 0; i < extraDisable.Count; i++)
        {
            var b = extraDisable[i];
            if (b != null) b.enabled = !paused;
        }

        ApplyCursorState();
    }

    private void ApplyCursorState()
    {
        bool anyMenuOpen = _isPaused || InventoryMainUI.IsOpen;

        if (anyMenuOpen)
        {
            if (unlockCursorOnOpen)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            return;
        }
        if (lockCursorWhenPlaying)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void OnButton_Play()
    {
        Resume();
    }

    public void OnButton_ExitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void OnButton_SaveAndQuit()
    {
        if (GameDataManager.Instance != null)
            GameDataManager.Instance.SaveGame();

        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void OnButton_QuitApp()
    {
        Application.Quit();
    }
}
