using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "GameSceneShiba"; // ใส่ชื่อ Scene เกมจริงของคุณ

    [Header("Buttons")]
    [SerializeField] private Button newButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;   // Panel ที่มี Master/Music/SFX
                                                         // ถ้าไม่มีปล่อยว่างได้

    private void Awake()
    {
        if (newButton) newButton.onClick.AddListener(OnNewClicked);
        if (loadButton) loadButton.onClick.AddListener(OnLoadClicked);
        if (settingsButton) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (exitButton) exitButton.onClick.AddListener(OnExitClicked);

        if (settingsPanel) settingsPanel.SetActive(false);
    }

    // ===== NEW GAME =====
    private void OnNewClicked()
    {
        // เริ่มใหม่จริง ๆ ก็ลบเซฟเก่าไปเลย (ถ้าไม่อยากลบ ก็เอาบรรทัดนี้ออก)
        SaveSystem.DeleteSave();

        SaveSystem.LoadOnStart = false; // ให้ GameManager ใน GameScene เริ่มค่าใหม่
        SceneManager.LoadScene(gameSceneName);
    }

    // ===== LOAD GAME =====
    private void OnLoadClicked()
    {
        if (!SaveSystem.SaveExists())
        {
            Debug.Log("ยังไม่มีไฟล์เซฟให้โหลด");
            return;
        }

        SaveSystem.LoadOnStart = true; // ให้ GameScene โหลดจากเซฟตอน Start
        SceneManager.LoadScene(gameSceneName);
    }

    // ===== SETTINGS =====
    private void OnSettingsClicked()
    {
        if (settingsPanel)
            settingsPanel.SetActive(true);
    }

    public void OnSettingsBack()
    {
        if (settingsPanel)
            settingsPanel.SetActive(false);
    }

    // ===== EXIT =====
    private void OnExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
