using UnityEngine;

public class ShopTrigger : MonoBehaviour
{
    [Header("Refs")]
    public ShopDefinition catalog;
    public ShopUI shopUI;
    [Header("Prompt")]
    public GameObject promptPanel;       // UI §”„∫È°¥ E (optional)
    public KeyCode interactKey = KeyCode.E;

    bool _playerInRange;

    void Awake()
    {
        if (!shopUI) shopUI = FindObjectOfType<ShopUI>(true);
        if (promptPanel) promptPanel.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;
        if (promptPanel) promptPanel.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        if (promptPanel) promptPanel.SetActive(false);
    }

    void Update()
    {
        if (!_playerInRange || shopUI == null || catalog == null) return;
        if (Input.GetKeyDown(interactKey))
        {
            shopUI.Open(catalog);
        }
    }
}
