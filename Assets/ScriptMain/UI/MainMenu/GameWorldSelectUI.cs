using UnityEngine;
using UnityEngine.UI;

public class GameWorldSelectUI : MonoBehaviour
{
    [Header("Slot Setup")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private SlotItemUI slotItemPrefab;
    [SerializeField] private int totalSlots = 3;

    [Header("Buttons")]
    [SerializeField] private Button loadButton;

    private SlotItemUI _selectedSlot;
    private SlotItemUI[] _slots;

    private void Start()
    {
        BuildSlots();
        loadButton.interactable = false;   // nothing selected yet
    }

    private void BuildSlots()
    {
        _slots = new SlotItemUI[totalSlots];

        for (int i = 0; i < totalSlots; i++)
        {
            var slot = Instantiate(slotItemPrefab, slotContainer);
            var preview = SaveSlotReader.ReadSlot(i);
            slot.Populate(i, preview);

            // capture i for lambda
            int index = i;
            slot.GetComponent<Button>().onClick.AddListener(() => OnSlotClicked(_slots[index]));

            _slots[i] = slot;
        }
    }

    private void OnSlotClicked(SlotItemUI slot)
    {
        // Deselect previous
        if (_selectedSlot != null)
            _selectedSlot.SetSelected(false);

        _selectedSlot = slot;
        _selectedSlot.SetSelected(true);

        // Load button only active if slot has data
        loadButton.interactable = slot.HasData;
    }

    // ── Buttons ──────────────────────────────────────────────
    public void OnLoadButtonClick()
    {
        if (_selectedSlot == null || !_selectedSlot.HasData) return;

        GlobalSaveContext.Instance.RequestLoad(_selectedSlot.SlotIndex);
        UIManager.Instance.LoadScene(GameScene.Game);
    }

    public void OnBackButtonClick()
    {
        UIManager.Instance.LoadScene(GameScene.GameMode);
    }
}
