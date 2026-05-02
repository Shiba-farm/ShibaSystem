using UnityEngine;

public static class AppBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        if(Object.FindFirstObjectByType<InputHandler>() != null) return;

        GameObject prefab = Resources.Load<GameObject>("GlobalManagers");

        if(prefab != null)
        {
            GameObject instance = Object.Instantiate(prefab);
            Object.DontDestroyOnLoad(instance);
            Debug.Log("Global Managers spawned and ready!");
        }
    }
}
