using UnityEngine;

/// <summary>
/// ใส่ไว้บน Workbench GameObject ในฉาก
/// ผู้เล่นเดินเข้าใกล้ → กด E → เปิด CraftingUI
/// </summary>
public class WorkbenchInteraction : MonoBehaviour, IInteractable
{
    public PromptType InteractPromptType => PromptType.CraftTable;

    public void Interact()
    {
        InGameUIManager.Instance.OpenExclusivePanel("Crafting");
    }
}
