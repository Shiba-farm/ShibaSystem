using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SummaryPanelUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject briefPanel;
    [SerializeField] private GameObject detailPanel;

    [Header("Brief")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private Transform categoryContainer;
    [SerializeField] private GameObject categoryRowPrefab;
    [SerializeField] private TextMeshProUGUI grandTotalText;
    [SerializeField] private Button sleepButton;

    [Header("Detail")]
    [SerializeField] private TextMeshProUGUI detailCategoryTitle;
    [SerializeField] private Transform detailContainer;
    [SerializeField] private GameObject itemRowDetailPrefab;
    [SerializeField] private TextMeshProUGUI detailTotalText;
    [SerializeField] private Button backButton;

    private void Start()
    {
        sleepButton.onClick.AddListener(OnSleepClicked);
        backButton.onClick.AddListener(ShowBrief);
    }

    public void OnEnable()
    {
        PopulateBrief();
        ShowBrief();
    }

    // --- Brief Panel ---

    private void PopulateBrief()
    {
        dayText.text = $"Day {GameDataManager.Instance.CurrentDayNumber}";
        grandTotalText.text = $"Total : {GameDataManager.Instance.GetNightTotalGold()}";

        // Clear old entries
        foreach (Transform child in categoryContainer)
            Destroy(child.gameObject);

        foreach (var record in GameDataManager.Instance.NightSellSummary.Values)
        {
            if (record.Items.Count == 0) continue; // skip empty categories

            var row = Instantiate(categoryRowPrefab, categoryContainer);
            row.GetComponent<CategoryRowUI>().Setup(record, OnCategoryClicked);
        }
    }

    private void ShowBrief()
    {
        briefPanel.SetActive(true);
        detailPanel.SetActive(false);
    }

    // --- Detail Panel ---

    private void OnCategoryClicked(CategorySellRecord record)
    {
        detailCategoryTitle.text = record.Category.ToString();
        detailTotalText.text = $"Total : {record.TotalGold}";

        foreach (Transform child in detailContainer)
            Destroy(child.gameObject);

        foreach (var item in record.Items)
        {
            var row = Instantiate(itemRowDetailPrefab, detailContainer);
            row.GetComponent<ItemRowDetailUI>().Setup(item);
        }

        briefPanel.SetActive(false);
        detailPanel.SetActive(true);
    }

    // --- Sleep ---

    private void OnSleepClicked()
    {
        GameDataManager.Instance.AdvanceDay();
        InGameUIManager.Instance.TogglePanel(InGamePanel.Summary);
    }
}
