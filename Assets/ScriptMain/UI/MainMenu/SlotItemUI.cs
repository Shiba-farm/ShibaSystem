using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlotItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI worldNameText;
    [SerializeField] private TextMeshProUGUI dateTimeText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI timeSpendText;
    [SerializeField] private GameObject emptyState;      // show when slot is empty
    [SerializeField] private GameObject filledState;     // show when slot has data
    [SerializeField] private Image backgroundImage;

    [Header("Selection Colors")]
    [SerializeField] private Color normalColor;
    [SerializeField] private Color selectedColor;

    private int _slotIndex;
    private bool _hasData;

    public int SlotIndex => _slotIndex;
    public bool HasData => _hasData;

    public void Populate(int slotIndex, SaveSlotPreview preview)
    {
        _slotIndex = slotIndex;
        _hasData = preview != null;

        if (preview == null)
        {
            emptyState?.SetActive(true);
            filledState?.SetActive(false);
            return;
        }

        emptyState?.SetActive(false);
        filledState?.SetActive(true);

        worldNameText.text  = preview.worldName;
        dateTimeText.text   = $"Month {preview.world.currentMonth} · Day {preview.world.currentDay}";
        moneyText.text      = $"{preview.world.sharedGold}G";
        timeSpendText.text  = preview.savedAt;
    }

    public void SetSelected(bool selected)
    {
        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedColor : normalColor;
    }
}
