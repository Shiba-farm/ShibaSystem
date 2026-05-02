using UnityEngine;

public class NPCInteractable : MonoBehaviour
{
    [Header("Settings")]
    public string characterName = "NPC";
    public DialogueSO dialogueData;
    public float interactRadius = 3f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Visual Prompt (Optional)")]
    public GameObject interactIcon;

    private Transform player;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
        if (interactIcon) interactIcon.SetActive(false);
    }

    void Update()
    {
        if (!player) return;

        // 1. เช็คสถานะต่างๆ
        bool isTalking = DialogueManager.Instance.IsDialogueActive;
        float dist = Vector3.Distance(transform.position, player.position);
        bool inRange = dist <= interactRadius;

        // 2. จัดการไอคอน (โชว์เมื่อ "อยู่ในระยะ" และ "ไม่ได้คุยอยู่")
        if (interactIcon)
        {
            bool shouldShow = inRange && !isTalking;

            // เช็คก่อนสั่งเพื่อลดการทำงานซ้ำซ้อน (Performance)
            if (interactIcon.activeSelf != shouldShow)
            {
                interactIcon.SetActive(shouldShow);
            }
        }

        // 3. จัดการ Input
        if (isTalking)
        {
            // ถ้ากำลังคุย -> กดเพื่อไปประโยคต่อไป
            if (Input.GetKeyDown(interactKey) || Input.GetMouseButtonDown(0))
            {
                DialogueManager.Instance.DisplayNextSentence();
            }
        }
        else if (inRange)
        {
            // ถ้าอยู่ในระยะและยังไม่คุย -> กดเพื่อเริ่มคุย
            if (Input.GetKeyDown(interactKey))
            {
                // หันหน้าหาผู้เล่น
                Vector3 direction = (player.position - transform.position).normalized;
                direction.y = 0;
                transform.rotation = Quaternion.LookRotation(direction);

                // เริ่มบทสนทนา
                DialogueManager.Instance.StartDialogue(dialogueData);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}