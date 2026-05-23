using TMPro;
using UnityEngine;

public class CreateNewGameUI : MonoBehaviour
{
    [Header("World Name")]
    [SerializeField] private TMP_InputField worldNameInputField;

    [Header("Player Amount")]
    [SerializeField] private TextMeshProUGUI playerAmountText;
    [SerializeField] private int minPlayers = 1;
    [SerializeField] private int maxPlayers = 4;

    private int _playerAmount = 1;

    private void Start()
    {
        UpdatePlayerAmountText();
    }

    // ── Player Amount ────────────────────────────────────────
    public void OnDecreaseButtonClick()
    {
        _playerAmount = Mathf.Max(minPlayers, _playerAmount - 1);
        UpdatePlayerAmountText();
    }

    public void OnIncreaseButtonClick()
    {
        _playerAmount = Mathf.Min(maxPlayers, _playerAmount + 1);
        UpdatePlayerAmountText();
    }

    private void UpdatePlayerAmountText()
    {
        playerAmountText.text = _playerAmount.ToString();
    }

    // ── Buttons ──────────────────────────────────────────────
    public void OnCreateButtonClick()
    {
        string worldName = worldNameInputField.text.Trim();

        if (string.IsNullOrEmpty(worldName))
        {
            Debug.LogWarning("[CreateNewGame] World name is empty.");
            return;
        }

        int slot = GlobalSaveContext.Instance.GetNextAvailableSlot();
        if (slot == -1)
        {
            Debug.LogWarning("[CreateNewGame] No available save slots.");
            return; // optionally show a UI message
        }

        // Store config in GlobalSaveContext before loading game scene
        GlobalSaveContext.Instance.RequestNewGame(slot, worldName);
        // TODO: pass worldName and playerAmount to GlobalSaveContext when ready

        UIManager.Instance.LoadScene(GameScene.MainGame);
    }

    public void OnBackButtonClick()
    {
        UIManager.Instance.LoadScene(GameScene.GameMode);
    }
}
