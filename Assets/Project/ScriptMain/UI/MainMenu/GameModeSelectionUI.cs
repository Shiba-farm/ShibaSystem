using UnityEngine;

public class GameModeSelectionUI : MonoBehaviour
{
    public void OnContinueButtonClick()
    {
        UIManager.Instance.LoadScene(GameScene.GameWorldSelect);
    }

    public void OnNewGameButtonClick()
    {
        UIManager.Instance.LoadScene(GameScene.CreateNewGame);
    }

    public void OnBackButtonClick()
    {
        UIManager.Instance.LoadScene(GameScene.MainMenu);
    }
}
