using UnityEngine;

public class InteractPromptUI : MonoBehaviour
{
    [SerializeField] GameObject bedPrompt;
    [SerializeField] GameObject craftPrompt;
    [SerializeField] GameObject shopPrompt;
    [SerializeField] GameObject debtPrompt;

    Transform trackedTarget;

    void OnEnable()
    {
        InteractController.OnInteractableFound += ShowPrompt;
        InteractController.OnInteractableLost += HidePrompt;
    }

    void OnDisable()
    {
        InteractController.OnInteractableFound -= ShowPrompt;
        InteractController.OnInteractableLost -= HidePrompt;
    }

    void ShowPrompt(IInteractable interactable, Transform target)
    {
        trackedTarget = target;

        // Hide all first
        bedPrompt.SetActive(false);
        craftPrompt.SetActive(false);
        shopPrompt.SetActive(false);
        debtPrompt.SetActive(false);

        // Show the right one
        switch (interactable.InteractPromptType)
        {
            case PromptType.Bed:         bedPrompt.SetActive(true);     break;
            case PromptType.CraftTable:  craftPrompt.SetActive(true);   break;
            case PromptType.Shop:        shopPrompt.SetActive(true);    break;
            case PromptType.Debt:        debtPrompt.SetActive(true);    break;
        }
    }

    void HidePrompt()
    {
        trackedTarget = null;
        bedPrompt.SetActive(false);
        craftPrompt.SetActive(false);
        shopPrompt.SetActive(false);
        debtPrompt.SetActive(false);
    }

    void Update()
    {
        if (trackedTarget == null) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(trackedTarget.position);
        screenPos.z = 0f;
        transform.position = screenPos;
    }
}
