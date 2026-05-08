using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebtPayUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject payPanel;
    [SerializeField] private GameObject punishmentPanel;
    [SerializeField] private GameObject summaryPanel;

    private void Start()
    {
        punishmentPanel.SetActive(false);
        payPanel.SetActive(true);
        summaryPanel.SetActive(false);
    }

    public void OnPunishClicked()
    {
        punishmentPanel.SetActive(true);
        payPanel.SetActive(false);
        summaryPanel.SetActive(false);
    }
}
