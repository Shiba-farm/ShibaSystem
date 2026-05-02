using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI Components")]
    public GameObject dialoguePanel;      // ��ǡ��ͧ��ͤ��������� (Panel)
    public Image portraitImage;           // �ٻ NPC ��ҹ����
    public TextMeshProUGUI nameText;      // ���� NPC
    public TextMeshProUGUI dialogueText;  // ��ͤ����ٴ

    [Header("Settings")]
    public float typeSpeed = 0.02f;       // �������ǵ��˹ѧ������

    private Queue<DialogueLine> sentences;
    private bool isTyping = false;
    private string currentFullText = "";

    // ������͡�������Ҥ������ (Player �����Ѻ�����)
    public bool IsDialogueActive { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Debug.LogWarning($"[DialogueManager] พบ Instance ซ้ำบน '{gameObject.name}' — ลบ Component"); Destroy(this); return; }
        Instance = this;

        sentences = new Queue<DialogueLine>();
        if (dialoguePanel) dialoguePanel.SetActive(false);
    }

    public void StartDialogue(DialogueSO dialogue)
    {
        IsDialogueActive = true;
        if (dialoguePanel) dialoguePanel.SetActive(true);

        // ��駪��� NPC
        if (nameText) nameText.text = dialogue.npcName;

        // ���������¤��� ����������¤��������
        sentences.Clear();
        foreach (var line in dialogue.lines)
        {
            sentences.Enqueue(line);
        }

        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        // ��ҡ��ѧ��������� ��顴������ʴ�����騺�ѹ��
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = currentFullText;
            isTyping = false;
            return;
        }

        // ��һ���¤������� ��騺���ʹ���
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = sentences.Dequeue();

        // 1. ����¹�ٻ Portrait
        if (portraitImage != null)
        {
            if (line.portrait != null)
            {
                portraitImage.sprite = line.portrait;
                portraitImage.gameObject.SetActive(true);
            }
            // ���������ٻ㹻���¤��� ������͹ �������ٻ������� (㹷�������͡���ٻ��������������)
            // else portraitImage.gameObject.SetActive(false); 
        }

        // 2. ������ͤ��� (Typewriter Effect)
        currentFullText = line.text;
        StopAllCoroutines();
        StartCoroutine(TypeSentence(line.text));
    }

    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typeSpeed);
        }
        isTyping = false;
    }

    void EndDialogue()
    {
        IsDialogueActive = false;
        if (dialoguePanel) dialoguePanel.SetActive(false);
    }
}