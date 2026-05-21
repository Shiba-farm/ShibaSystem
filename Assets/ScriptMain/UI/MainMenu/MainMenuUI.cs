using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public void OnButtonPlayClick()
    {
        UIManager.Instance.LoadScene(GameScene.GameMode);
        Debug.Log("Play button click");
    }

    public void OnButtonSettingClick()
    {
        UIManager.Instance.LoadScene(GameScene.GameOption);
        Debug.Log("Setting button click");
    }

    public void OnButtonQuitClick()
    {
        Application.Quit();
    }
}
