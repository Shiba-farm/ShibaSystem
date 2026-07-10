using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameScene
{
    MainMenu,
    GameOption,
    GameWorldSelect,
    CreateNewGame,
    GameMode,
    MainGame,
    DunGeon,
    RoomShiba,
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Transition")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;
    public float FadeDuration => fadeDuration;

    [Header("Scenes")]
    [SerializeField]
    private string[] sceneNames =
    {
        "MainMenu",       // 0 — GameScene.MainMenu
        "GameOption",     // 1 — GameScene.GameOption
        "GameWorldSelect",// 2 — GameScene.GameWorldSelect
        "CreateNewGame",  // 3 — GameScene.CreateNewGame
        "GameMode",       // 4 — GameScene.GameMode
        "MainGame",            // 5 — GameScene.Game
        "Dungeon",            // 5 — GameScene.Game
        "RoomShiba"            // 5 — GameScene.Game
    };
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject); // คงอยู่ข้ามทุก scene
    }


    // ── Public API ───────────────────────────────────────────
    public void LoadScene(GameScene scene)
    {
        Debug.Log($"Game scene selected : {(int)scene}");
        StartCoroutine(TransitionToScene(sceneNames[(int)scene]));
    }

    public void LoadScneneByName(string sceneName)
    {
        StartCoroutine(TransitionToScene(sceneName));
    }

    private IEnumerator TransitionToScene(string sceneName)
    {
        // Fade out
        yield return StartCoroutine(Fade(0f, 1f));

        SceneManager.LoadScene(sceneName);

        // Wait one frame for scene to load
        yield return null;

        // Fade in
        yield return StartCoroutine(Fade(1f, 0f));
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        fadeCanvasGroup.blocksRaycasts = true;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;   // unscaled — works even if timeScale is 0
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = to;
        fadeCanvasGroup.blocksRaycasts = to > 0f;
    }

    public void FadeOut()
    {
        StopAllCoroutines();
        StartCoroutine(Fade(fadeCanvasGroup.alpha, 1f));
    }

    public void FadeIn()
    {
        StopAllCoroutines();
        StartCoroutine(Fade(fadeCanvasGroup.alpha, 0f));
    }
}
