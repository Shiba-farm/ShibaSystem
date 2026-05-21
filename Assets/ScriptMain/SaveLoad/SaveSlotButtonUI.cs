using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SaveSlotButtonUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI slotLabel;
    [SerializeField] private TextMeshProUGUI metaLabel;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button newGameButton;

    private int _slot;

    public void Populate(int slot, SaveSlotPreview preview)
    {
        _slot = slot;

        if (preview == null)
        {
            slotLabel.text = $"Slot {slot + 1}";
            metaLabel.text = "Empty";
            loadButton.gameObject.SetActive(false);
            newGameButton.gameObject.SetActive(true);
        }
        else
        {
            slotLabel.text = $"Slot {slot + 1}";
            metaLabel.text = $"Month {preview.world.currentMonth} · Day {preview.world.currentDay}";
            loadButton.gameObject.SetActive(true);
            newGameButton.gameObject.SetActive(true);
        }
    }

    public void OnLoadClick()
    {
        GlobalSaveContext.Instance.RequestLoad(_slot);
        NetworkManager.Singleton.StartHost();
        SceneManager.LoadScene("GameScene");
    }

    public void OnNewGameClick()
    {
        GlobalSaveContext.Instance.RequestNewGame(_slot);
        NetworkManager.Singleton.StartHost();
        SceneManager.LoadScene("GameScene");
    }
}
