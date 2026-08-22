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

    [Header("Special System Button (Optional)")]
    [Tooltip("ปุ่มตัวเลือกที่ 3 ในกล่องสนทนา — โผล่เฉพาะตอน NPC ที่กำลังคุยด้วยมี specialSystemPanel " +
             "กำหนดไว้ (เช่น NPC พ่อค้า → ปุ่ม \"ซื้อของ\") กดแล้วจะปิด dialogue แล้วเปิด panel นั้นแทน " +
             "ถ้าไม่ผูกไว้ ฟีเจอร์นี้จะถูกข้ามไปเฉยๆ ไม่กระทบ NPC ที่ไม่มีระบบพิเศษ")]
    [SerializeField] private Button specialSystemButton;

    private Queue<DialogueLine> sentences;
    private bool isTyping = false;
    private string currentFullText = "";
    private CanvasGroup _canvasGroup; // InGameUIManager สั่ง alpha=0/interactable=false ไว้ตอน Awake ต้องเปิดคืนเองตอน StartDialogue
    private InGamePanel? _currentSpecialSystem; // panel พิเศษของ NPC ที่กำลังคุยด้วยอยู่ตอนนี้ (ถ้ามี)

    // ������͡�������Ҥ������ (Player �����Ѻ�����)
    public bool IsDialogueActive { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Debug.LogWarning($"[DialogueManager] พบ Instance ซ้ำบน '{gameObject.name}' — ลบ Component"); Destroy(this); return; }
        Instance = this;

        _canvasGroup = GetComponent<CanvasGroup>();
        sentences = new Queue<DialogueLine>();
        if (dialoguePanel) dialoguePanel.SetActive(false);
        SetPanelVisible(false);

        if (specialSystemButton)
        {
            specialSystemButton.onClick.AddListener(OnSpecialSystemButtonClicked);
            specialSystemButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// InGameUIManager.InitializePanels() จะตั้ง CanvasGroup.alpha = 0 / interactable = false
    /// ให้ทุก panel (รวมถึง Dialogue) ตอน Awake — ถ้าเราแค่ SetActive(true) เฉยๆ
    /// GameObject จะ active จริงแต่มองไม่เห็นเพราะ alpha ยังเป็น 0 อยู่ ต้องเปิดคืนเองตรงนี้
    /// </summary>
    private void SetPanelVisible(bool visible)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    /// <summary>
    /// เริ่มบทสนทนาปกติ — ผูก specialSystem (optional) ถ้า NPC คนนี้มีระบบพิเศษ (เช่น ร้านค้า)
    /// ให้โชว์ปุ่มตัวเลือกที่ 3 ในกล่องสนทนา
    /// </summary>
    public void StartDialogue(DialogueSO dialogue, InGamePanel? specialSystem = null)
    {
        StartDialogueInternal(dialogue.npcName, dialogue.lines, specialSystem);
    }

    /// <summary>
    /// เริ่มบทพูดสั้นๆ แบบไดนามิก ไม่ต้องสร้าง DialogueSO asset ล่วงหน้า —
    /// ใช้กับข้อความชั่วคราว เช่น NPC พูดขอบคุณตอนได้รับของขวัญ (ไม่มีปุ่มระบบพิเศษ)
    /// </summary>
    public void StartOneLiner(string speakerName, string text, Sprite portrait = null)
    {
        StartDialogueInternal(speakerName, new[] { new DialogueLine { text = text, portrait = portrait } }, null);
    }

    private void StartDialogueInternal(string npcName, DialogueLine[] lines, InGamePanel? specialSystem)
    {
        IsDialogueActive = true;
        if (dialoguePanel) dialoguePanel.SetActive(true);
        SetPanelVisible(true);

        // ปุ่มตัวเลือกที่ 3 (optional) — โผล่เฉพาะตอน NPC นี้มี specialSystem กำหนดไว้
        _currentSpecialSystem = specialSystem;
        if (specialSystemButton) specialSystemButton.gameObject.SetActive(specialSystem.HasValue);

        // ตั้งชื่อ NPC
        if (nameText) nameText.text = npcName;

        // เคลียร์คิวเก่า แล้วใส่คิวใหม่
        sentences.Clear();
        foreach (var line in lines)
        {
            sentences.Enqueue(line);
        }

        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = currentFullText;
            isTyping = false;
            return;
        }

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
        SetPanelVisible(false);

        _currentSpecialSystem = null;
        if (specialSystemButton) specialSystemButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// เรียกตอนผู้เล่นกดปุ่มตัวเลือกที่ 3 (ผูกไว้ผ่าน specialSystemButton.onClick ใน Awake) —
    /// ปิด dialogue แล้วเปิด panel ระบบพิเศษของ NPC ที่กำลังคุยด้วยแทน (เช่น หน้าร้านค้า)
    /// </summary>
    private void OnSpecialSystemButtonClicked()
    {
        if (!_currentSpecialSystem.HasValue) return;
        InGamePanel target = _currentSpecialSystem.Value;

        EndDialogue();

        if (InGameUIManager.Instance != null)
            InGameUIManager.Instance.OpenExclusivePanel(target);
        else
            Debug.LogWarning("[DialogueManager] InGameUIManager.Instance เป็น null — เปิด special system panel ไม่ได้");
    }
}