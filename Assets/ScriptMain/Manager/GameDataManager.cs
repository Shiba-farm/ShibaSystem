using UnityEngine;

public enum GameState { Playing, InMenu, Pause }
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }
    public GameState CurrentState { get; private set; }
    public ItemDatabases itemDatabases;
    public CraftRecipeDatabase craftRecipeDatabase;
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        itemDatabases.Initialize();
        craftRecipeDatabase.Initialize();
    }

    public void SetGameState(GameState gameState)
    {
        CurrentState = gameState;
    }
    public void SaveGame()
    {
        // Implement your save logic here
        Debug.Log("Game saved!");
    }

}
