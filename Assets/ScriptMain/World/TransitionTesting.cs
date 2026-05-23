using UnityEngine;

public class TransitionTesting : MonoBehaviour, IInteractable
{
    public PromptType InteractPromptType => PromptType.Mine;

    public void Interact()
    {
        SceneTransitionManager.Instance?.LoadNetworkScene("Dungeon");
    }
}
