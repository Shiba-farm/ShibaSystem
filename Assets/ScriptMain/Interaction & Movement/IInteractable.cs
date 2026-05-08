using UnityEngine;

public interface IInteractable
{
    public void Interact();
    PromptType InteractPromptType { get; }
}

public enum PromptType
{
    None,
    Bed,
    CraftTable,
    Shop,
    Debt,
}