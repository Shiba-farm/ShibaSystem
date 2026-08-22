using UnityEngine;

[System.Serializable]
public struct DialogueLine
{
    [Tooltip("รูปอารมณ์ของ NPC ในประโยคนี้")]
    public Sprite portrait;

    [TextArea(3, 5)]
    public string text;
}

[CreateAssetMenu(menuName = "NPC/Dialogue")]
public class DialogueSO : ScriptableObject
{
    [Header("Info")]
    public string npcName;

    [Header("Conversation")]
    public DialogueLine[] lines;
}