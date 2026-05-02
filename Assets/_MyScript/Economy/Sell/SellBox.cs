using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SellBox — เปิดกล่องแล้วลากของจาก Inventory ไปขาย
///
/// Flow:
/// 1. Player เข้าใกล้กล่อง → prompt "กด E เพื่อเปิดกล่องขาย"
/// 2. กด E → Animation "Open" + เปิด UI
/// 3. UI ซ้าย: Inventory items (คลิกเพื่อใส่กล่อง)
/// 4. UI ขวา: ของที่อยู่ในกล่อง (staged) + ราคารวม
/// 5. ปุ่ม "ขาย" → ขายของทั้งหมดในกล่อง
/// 6. กด E / ปุ่ม Close → ปิด UI + Animation "Close"
///
/// UI Setup ใน Canvas:
///   SellPanel
///     ├── InventoryPanel (ซ้าย)
///     │     ├── Title (TMP — "Inventory")
///     │     └── InventorySlotsParent (GridLayoutGroup)
///     ├── StagedPanel (ขวา — "กล่องขาย")
///     │     ├── Title (TMP — "กล่องขาย")
///     │     ├── StagedSlotsParent (VerticalLayoutGroup)
///     │     ├── TotalPriceText (TMP — "รวม: ¥0")
///     │     ├── SellButton (Button — "ขาย")
///     │     └── ClearButton (Button — "เคลียร์")
///     └── CloseButton (Button — "ปิด")
/// </summary>
public class SellBox : MonoBehaviour
{
    // ─── Keys ─────────────────────────────────────────────────────────
    [Header("Keys")]
    public KeyCode openKey = KeyCode.E;

    // ─── Box Animation ────────────────────────────────────────────────
    [Header("Box Animation")]
    [Tooltip("Animator ของ model กล่อง — ต้องมี trigger 'Open' และ 'Close'")]
    public Animator boxAnimator;

    [Header("Price")]
    public float sellMultiplier = 1f;

    // ─── Prompt ───────────────────────────────────────────────────────
    [Header("Prompt UI")]
    public GameObject promptPanel;
    public TextMeshProUGUI promptLabel;

    // ─── Sell Panel ───────────────────────────────────────────────────
    [Header("Sell Panel")]
    public GameObject sellPanel;

    [Header("  Inventory Side (ซ้าย)")]
    [Tooltip("Parent ที่ spawn ปุ่ม inventory item (GridLayoutGroup)")]
    public Transform inventorySlotsParent;
    [Tooltip("Prefab ปุ่ม item — ต้องมี Image ชื่อ 'Icon', TMP ชื่อ 'Label'")]
    public GameObject inventoryItemButtonPrefab;

    [Header("  Staged Side (ขวา — กล่องขาย)")]
    [Tooltip("Parent ที่ spawn staged item rows (VerticalLayoutGroup)")]
    public Transform stagedSlotsParent;
    [Tooltip("Prefab row staged — ต้องมี Image 'Icon', TMP 'Label', Button 'RemoveBtn'")]
    public GameObject stagedRowPrefab;
    public TextMeshProUGUI totalPriceText;
    public Button sellButton;
    public Button clearButton;
    public Button closeButton;

    // ─── Feedback ─────────────────────────────────────────────────────
    [Header("Feedback")]
    public TextMeshProUGUI feedbackLabel;
    public float feedbackDuration = 2.5f;

    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip openSfx;
    public AudioClip sellSfx;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    // ─── Runtime ──────────────────────────────────────────────────────
    bool _playerInRange;
    bool _isOpen;
    float _feedbackTimer;

    // staged items: item + amount ที่รอขาย
    readonly List<StagedItem> _staged = new List<StagedItem>();
    readonly List<GameObject> _spawnedInvButtons  = new List<GameObject>();
    readonly List<GameObject> _spawnedStagedRows  = new List<GameObject>();

    [System.Serializable]
    class StagedItem
    {
        public ItemSO item;
        public int amount;
        public StagedItem(ItemSO i, int a) { item = i; amount = a; }
    }

    // ──────────────────────────────────────────────────────────────────
    void Start()
    {
        if (promptPanel) promptPanel.SetActive(false);
        if (sellPanel)   sellPanel.SetActive(false);
        if (feedbackLabel) feedbackLabel.text = "";

        if (sellButton)  sellButton.onClick.AddListener(OnSellPressed);
        if (clearButton) clearButton.onClick.AddListener(OnClearPressed);
        if (closeButton) closeButton.onClick.AddListener(OnClosePressed);
    }

    // ─── Trigger ──────────────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;
        ShowPrompt($"กด [{openKey}] เพื่อเปิดกล่องขาย");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        if (promptPanel) promptPanel.SetActive(false);
        if (_isOpen) CloseBox();
    }

    // ─── Update ───────────────────────────────────────────────────────

    void Update()
    {
        // Feedback timer
        if (_feedbackTimer > 0)
        {
            _feedbackTimer -= Time.deltaTime;
            if (_feedbackTimer <= 0 && feedbackLabel) feedbackLabel.text = "";
        }

        if (!_playerInRange) return;

        if (Input.GetKeyDown(openKey))
        {
            if (_isOpen) CloseBox();
            else OpenBox();
        }

        if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
            CloseBox();
    }

    // ─── Open / Close ─────────────────────────────────────────────────

    void OpenBox()
    {
        _isOpen = true;
        if (promptPanel) promptPanel.SetActive(false);

        // Animation
        if (boxAnimator) boxAnimator.SetTrigger("Open");

        // SFX
        if (sfxSource && openSfx) sfxSource.PlayOneShot(openSfx, sfxVolume);

        // Unlock cursor
        Cursor.visible    = true;
        Cursor.lockState  = CursorLockMode.None;

        _staged.Clear();
        if (sellPanel) sellPanel.SetActive(true);

        RefreshInventoryPanel();
        RefreshStagedPanel();
    }

    void CloseBox()
    {
        // คืนของทั้งหมดใน staged กลับ Inventory ก่อนปิด (ยังไม่ได้ขาย)
        if (_staged.Count > 0)
            OnClearPressed();

        _isOpen = false;

        if (boxAnimator) boxAnimator.SetTrigger("Close");
        if (sellPanel)   sellPanel.SetActive(false);
        ShowPrompt($"กด [{openKey}] เพื่อเปิดกล่องขาย");

        // Lock cursor
        if (!InventoryMainUI.IsOpen)
        {
            Cursor.visible   = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // ─── Inventory Panel ──────────────────────────────────────────────

    void RefreshInventoryPanel()
    {
        foreach (var g in _spawnedInvButtons) if (g) Destroy(g);
        _spawnedInvButtons.Clear();

        if (inventorySlotsParent == null || inventoryItemButtonPrefab == null) return;

        // Inventory slots
        if (InventoryMainUI.Instance != null)
        {
            foreach (var slot in InventoryMainUI.Instance.slots)
            {
                if (slot == null || slot.item == null || slot.amount <= 0) continue;
                if (!slot.item.sellable) continue;
                SpawnInventoryButton(slot.item, slot.amount, isHotbar: false);
            }
        }

        // Hotbar slots
        if (HotbarUI.Instance != null)
        {
            foreach (var slot in HotbarUI.Instance.slots)
            {
                if (slot == null || slot.item == null || slot.amount <= 0) continue;
                if (!slot.item.sellable) continue;
                if (slot.item.category == ItemCategory.Tools) continue; // Tool ขายไม่ได้
                SpawnInventoryButton(slot.item, slot.amount, isHotbar: true);
            }
        }
    }

    void SpawnInventoryButton(ItemSO item, int amount, bool isHotbar)
    {
        var obj = Instantiate(inventoryItemButtonPrefab, inventorySlotsParent);
        _spawnedInvButtons.Add(obj);

        // Icon
        var icon = obj.transform.Find("Icon")?.GetComponent<Image>();
        if (icon && item.icon) icon.sprite = item.icon;

        // Label: ชื่อ + จำนวน + ราคา
        int price = GetSellPrice(item) * amount;
        var label = obj.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (label) label.text = $"{item.itemName} x{amount}\n¥{price:N0}";

        // Click → ใส่กล่อง
        var btn = obj.GetComponent<Button>();
        if (btn)
        {
            var captureItem   = item;
            var captureAmount = amount;
            var captureIsHotbar = isHotbar;
            btn.onClick.AddListener(() => MoveToBox(captureItem, captureAmount, captureIsHotbar));
        }
    }

    // ─── Staged Panel ─────────────────────────────────────────────────

    void RefreshStagedPanel()
    {
        foreach (var g in _spawnedStagedRows) if (g) Destroy(g);
        _spawnedStagedRows.Clear();

        int total = 0;
        foreach (var staged in _staged)
        {
            int rowPrice = GetSellPrice(staged.item) * staged.amount;
            total += rowPrice;

            if (stagedSlotsParent != null && stagedRowPrefab != null)
            {
                var row = Instantiate(stagedRowPrefab, stagedSlotsParent);
                _spawnedStagedRows.Add(row);

                var icon  = row.transform.Find("Icon")?.GetComponent<Image>();
                var label = row.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
                var removeBtn = row.transform.Find("RemoveBtn")?.GetComponent<Button>();

                if (icon && staged.item.icon) icon.sprite = staged.item.icon;
                if (label) label.text = $"{staged.item.itemName} x{staged.amount}  ¥{rowPrice:N0}";

                var capturedStaged = staged;
                removeBtn?.onClick.AddListener(() =>
                {
                    ReturnToInventory(capturedStaged);
                    RefreshInventoryPanel();
                    RefreshStagedPanel();
                });
            }
        }

        if (totalPriceText) totalPriceText.text = $"รวม: ¥{total:N0}";
        if (sellButton) sellButton.interactable = _staged.Count > 0;
    }

    // ─── Move To Box ──────────────────────────────────────────────────

    void MoveToBox(ItemSO item, int amount, bool fromHotbar)
    {
        // ดึงออกจาก Inventory / Hotbar
        if (fromHotbar)
        {
            if (HotbarUI.Instance != null)
                RemoveFromHotbar(item, amount);
        }
        else
        {
            if (InventoryMainUI.Instance != null)
                RemoveFromInventory(item, amount);
        }

        // เพิ่มใน staged
        var existing = _staged.Find(s => s.item == item);
        if (existing != null) existing.amount += amount;
        else _staged.Add(new StagedItem(item, amount));

        RefreshInventoryPanel();
        RefreshStagedPanel();
    }

    void ReturnToInventory(StagedItem staged)
    {
        _staged.Remove(staged);
        // คืนของกลับ Inventory
        if (InventoryMainUI.Instance != null)
            InventoryMainUI.Instance.AddItemToInventory(staged.item, staged.amount);
    }

    // ─── Sell ─────────────────────────────────────────────────────────

    void OnSellPressed()
    {
        if (_staged.Count == 0) { ShowFeedback("ไม่มีของในกล่อง!", Color.gray); return; }

        int total = 0;
        int totalItems = 0;

        foreach (var staged in _staged)
        {
            int price = GetSellPrice(staged.item) * staged.amount;
            total += price;
            totalItems += staged.amount;

            // บันทึกรายรับ
            DailyEconomyTracker.Instance?.RecordSale(staged.item, staged.amount, price);
            if (MarketPriceSystem.Instance != null)
                MarketPriceSystem.Instance.RecordSale(staged.item.itemName, staged.amount);
        }

        // ไม่จ่ายเงินทันที — รอจ่ายตอนจบวัน (DayEndSystem.FinishDay)
        _staged.Clear();

        PlaySfx(sellSfx);
        ShowFeedback($"วางขาย {totalItems} ชิ้น — รับ ¥{total:N0} ตอนจบวัน", new Color(0.2f, 0.8f, 0.2f));

        RefreshInventoryPanel();
        RefreshStagedPanel();
    }

    void OnClearPressed()
    {
        // คืนของทั้งหมดใน staged กลับ Inventory
        foreach (var staged in _staged)
            InventoryMainUI.Instance?.AddItemToInventory(staged.item, staged.amount);
        _staged.Clear();

        RefreshInventoryPanel();
        RefreshStagedPanel();
    }

    void OnClosePressed() => CloseBox();

    // ─── Remove Helpers ───────────────────────────────────────────────

    void RemoveFromInventory(ItemSO item, int amount)
    {
        if (InventoryMainUI.Instance == null) return;
        int remaining = amount;
        foreach (var slot in InventoryMainUI.Instance.slots)
        {
            if (remaining <= 0) break;
            if (slot == null || slot.item != item) continue;
            int take = Mathf.Min(remaining, slot.amount);
            slot.DecreaseAmount(take);
            remaining -= take;
        }
    }

    void RemoveFromHotbar(ItemSO item, int amount)
    {
        if (HotbarUI.Instance == null) return;
        int remaining = amount;
        foreach (var slot in HotbarUI.Instance.slots)
        {
            if (remaining <= 0) break;
            if (slot == null || slot.item != item) continue;
            int take = Mathf.Min(remaining, slot.amount);
            int newAmount = slot.amount - take;
            if (newAmount <= 0) slot.Clear();
            else slot.SetStack(slot.item, newAmount);
            remaining -= take;
        }
    }

    // ─── Price ────────────────────────────────────────────────────────

    int GetSellPrice(ItemSO item)
    {
        if (item == null || !item.sellable) return 0;
        if (MarketPriceSystem.Instance != null)
            return MarketPriceSystem.Instance.GetSellPrice(item, sellMultiplier);
        return Mathf.RoundToInt(item.sellPrice * sellMultiplier);
    }

    // ─── UI Helpers ───────────────────────────────────────────────────

    void ShowPrompt(string msg)
    {
        if (promptPanel) promptPanel.SetActive(true);
        if (promptLabel) promptLabel.text = msg;
    }

    void ShowFeedback(string msg, Color color)
    {
        if (!feedbackLabel) return;
        feedbackLabel.text  = msg;
        feedbackLabel.color = color;
        _feedbackTimer = feedbackDuration;
        Debug.Log($"[SellBox] {msg}");
    }

    void PlaySfx(AudioClip clip)
    {
        if (sfxSource && clip) sfxSource.PlayOneShot(clip, sfxVolume);
    }
}
