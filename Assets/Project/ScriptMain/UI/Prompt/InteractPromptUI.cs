using System.Collections.Generic;
using UnityEngine;

public class InteractPromptUI : MonoBehaviour
{
    // ── Serialized ────────────────────────────────────────────────────────────
    // แทนที่จะ hardcode 4 field แยก ใช้ Dictionary map PromptType → GameObject
    // เพิ่ม prompt ใหม่ใน Inspector ได้โดยไม่ต้องแก้โค้ด
    [System.Serializable]
    public struct PromptEntry
    {
        public PromptType type;
        public GameObject promptObject;
    }

    [SerializeField] private List<PromptEntry> prompts;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private Camera        _mainCamera;
    private Transform     _trackedTarget;
    private Dictionary<PromptType, GameObject> _promptMap;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Awake()
    {
        // Build lookup dictionary
        _promptMap = new Dictionary<PromptType, GameObject>(prompts.Count);
        foreach (var entry in prompts)
            if (entry.promptObject != null)
                _promptMap[entry.type] = entry.promptObject;
    }

    void Start()
    {
        // Cache ครั้งเดียวใน Start แทน Camera.main ใน Update ทุก frame
        _mainCamera = Camera.main;
    }

    void OnEnable()
    {
        InteractController.OnInteractableFound += ShowPrompt;
        InteractController.OnInteractableLost  += HidePrompt;
    }

    void OnDisable()
    {
        InteractController.OnInteractableFound -= ShowPrompt;
        InteractController.OnInteractableLost  -= HidePrompt;
    }

    // ── Prompt logic ──────────────────────────────────────────────────────────
    void ShowPrompt(IInteractable interactable, Transform target)
    {
        _trackedTarget = target;

        foreach (var kv in _promptMap)
            kv.Value.SetActive(kv.Key == interactable.InteractPromptType);
    }

    void HidePrompt()
    {
        _trackedTarget = null;
        foreach (var kv in _promptMap)
            kv.Value.SetActive(false);
    }

    // ── Position tracking ─────────────────────────────────────────────────────
    void Update()
    {
        if (_trackedTarget == null || _mainCamera == null) return;

        Vector3 screenPos = _mainCamera.WorldToScreenPoint(_trackedTarget.position);
        screenPos.z       = 0f;
        transform.position = screenPos;
    }
}
