using UnityEngine;
using Sirenix.OdinInspector; // ถ้ามี Odin ถ้าไม่มีให้ลบออกได้ครับ

[System.Serializable]
public struct DialogueLine
{
    [Tooltip("รูปอารมณ์ของ NPC ในประโยคนี้")]
    [PreviewField(60), TableColumnWidth(80, Resizable = false)]
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
    [TableList(ShowIndexLabels = true)]
    public DialogueLine[] lines;
}