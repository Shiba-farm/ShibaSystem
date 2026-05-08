using UnityEngine;

public class NPCDebt : MonoBehaviour, IInteractable
{

    public PromptType InteractPromptType => PromptType.Debt;

    private bool isDebtMonthActive = false; // set true when month ends

    public void Interact()
    {
        Debug.Log("NPC interact");
        InGameUIManager.Instance.OpenExclusivePanel("Debt");
    }
}
