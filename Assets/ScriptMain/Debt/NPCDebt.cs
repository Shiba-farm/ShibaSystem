using UnityEngine;

public class NPCDebt : MonoBehaviour, IInteractable
{

    public PromptType InteractPromptType => PromptType.Debt;

    [Header("Dialogue")]
    [SerializeField] private string settledDialogue = "You've already settled this month. Don't be late next time.";
    [SerializeField] private string noDebtDialogue = "You have no debt. Enjoy it while it lasts.";
    [SerializeField] private string unpaidDialogue = "You still owe me. Come to pay?";

    public void Interact()
    {
        if (DebtManager.Instance == null)
        {
            OpenDebtPanel();
            return;
        }

        if (DebtManager.Instance.CurrentDebt <= 0)
        {
            ShowDialogueOnly(noDebtDialogue);
            return;
        }

        if (DebtManager.Instance.IsMonthSettled)
        {
            ShowDialogueOnly(settledDialogue);
            return;
        }

        // Still has unpaid debt this month
        ShowDialogueOnly(unpaidDialogue, openPanelAfter: true);
    }

    private void ShowDialogueOnly(string message, bool openPanelAfter = false)
    {
        Debug.Log($"[NPCDebt] {message}");
        // Wire this to whatever dialogue/prompt system you have
        // e.g. DialogueManager.Instance.Show(message, onClose: () => { if (openPanelAfter) OpenDebtPanel(); });
        if (openPanelAfter) OpenDebtPanel();
    }

    private void OpenDebtPanel()
    {
        InGameUIManager.Instance.OpenExclusivePanel("Debt");
    }
}
