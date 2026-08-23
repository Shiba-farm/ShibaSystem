using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The "Title" block above the grid in the Animal Shop panel — shows the icon,
/// name, and price of whichever animal cell the player last clicked, and fires
/// the purchase request for it.
///
/// Populated by AnimalPanelUI.SelectAnimal(). Buy button click is wired here (self-
/// contained, same as AnimalCategoryRowUI wiring its own row button) and calls
/// AnimalStockServerManager.BuyLiveStockServerRpc directly with CurrentAnimal's
/// name — the server re-validates price/gold on its own before spawning anything,
/// so this is just the request; success/failure comes back via
/// AnimalStockServerManager.OnBuyLiveStockResult for whoever wants to show it
/// (not wired to any UI yet — no result toast/feedback exists).
/// </summary>
public class AnimalDetailUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button buyButton;

    public AnimalSO CurrentAnimal { get; private set; }
    public Button BuyButton => buyButton;

    private void Awake()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyButtonClicked);
    }

    private void OnBuyButtonClicked()
    {
        if (CurrentAnimal == null) return; // nothing selected yet (e.g. empty database edge case)

        if (AnimalStockServerManager.Instance == null)
        {
            Debug.LogWarning("[AnimalDetailUI] AnimalStockServerManager.Instance is null — can't request purchase.");
            return;
        }

        AnimalStockServerManager.Instance.BuyLiveStockServerRpc(CurrentAnimal.animalName);
    }

    public void Setup(AnimalSO animal)
    {
        CurrentAnimal = animal;

        if (iconImage != null)
        {
            iconImage.sprite = animal.icon;
            iconImage.enabled = animal.icon != null;
        }

        if (nameText != null) nameText.text = animal.animalName;
        if (priceText != null) priceText.text = $"{animal.price} G"; // adjust currency label to match your Gold UI elsewhere
    }

    /// <summary>Resets to the empty placeholder state — called whenever the category changes.</summary>
    public void Clear()
    {
        CurrentAnimal = null;

        if (iconImage != null) iconImage.enabled = false;
        if (nameText != null) nameText.text = "Animal Title";
        if (priceText != null) priceText.text = "Animal Price";
    }
}
