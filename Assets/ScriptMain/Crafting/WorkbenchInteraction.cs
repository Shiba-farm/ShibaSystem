using UnityEngine;

/// <summary>
/// ใส่ไว้บน Workbench GameObject ในฉาก
/// ผู้เล่นเดินเข้าใกล้ → กด E → เปิด CraftingUI
/// </summary>
public class WorkbenchInteraction : MonoBehaviour, IInteractable
{
    [Header("Config")]
    [Tooltip("ระยะที่ผู้เล่นต้องเข้ามาใกล้เพื่อใช้งาน")]
    public float interactDistance = 2.5f;

    [Tooltip("ปุ่มกด interact")]
    public KeyCode interactKey = KeyCode.E;

    [Tooltip("ระดับโต๊ะคราฟ (สูตรบางอันต้องใช้ Level สูง)")]
    public int workbenchLevel = 0;

    [Header("UI Prompt")]
    [Tooltip("GameObject แสดงข้อความ 'กด E เพื่อคราฟ' (Optional)")]
    public GameObject promptUI;

    Transform playerTransform;
    bool playerInRange;

    void Start()
    {
        if (promptUI) promptUI.SetActive(false);

        // หา Player
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player) playerTransform = player.transform;
    }

    void Update()
    {
        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool inRange = dist <= interactDistance;

        // แสดง/ซ่อน prompt
        if (inRange != playerInRange)
        {
            playerInRange = inRange;
            if (promptUI) promptUI.SetActive(inRange);
        }

        // กด E เพื่อเปิด
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            if (CraftingUI.Instance != null)
            {
                CraftingUI.Instance.workbenchLevel = workbenchLevel;
                CraftingUI.Instance.Open();
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }

    public void Interact()
    {
        InGameUIManager.Instance.OpenExclusivePanel("Crafting");
    }
}
