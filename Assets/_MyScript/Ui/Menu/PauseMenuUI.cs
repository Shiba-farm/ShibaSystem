using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;

    // ── Settings Panel ───────────────────────────────────────
    public void OnButton_OpenSettings()
    {
        settingsPanel?.SetActive(true);
    }

    public void OnButton_CloseSettings()
    {
        settingsPanel?.SetActive(false);
    }

    // ── Pause Panel buttons ──────────────────────────────────
    public void OnButton_Play()
    {
        InGameUIManager.Instance?.OpenExclusivePanel("Pause");
    }

    public void OnButton_Save()
    {
        SaveLoadManager.Instance?.SaveGame();
    }

    public void OnButton_SaveAndQuit()
    {
        SaveLoadManager.Instance?.SaveGame();
        SceneManager.LoadScene("MainMenu");
    }

    public void OnButton_QuitApp() => Application.Quit();
}