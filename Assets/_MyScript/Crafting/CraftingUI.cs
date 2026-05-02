using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingUI : MonoBehaviour
{
    public static CraftingUI Instance { get; private set; }

    [Header("Panels")]
    public GameObject craftingPanel;

    [Header("Recipe List (ซ้าย)")]
    [Tooltip("Parent ที่จะ Spawn ปุ่มสูตร")]
    public Transform recipeListParent;
    [Tooltip("Prefab ปุ่มสูตร")]
    public GameObject recipeButtonPrefab;

    [Header("Recipe Detail (ขวา)")]
    public Image selectedIcon;
    public TextMeshProUGUI selectedNameText;
    public TextMeshProUGUI selectedDescText;
    public Transform ingredientListParent;
    public GameObject ingredientRowPrefab;
    public TextMeshProUGUI resultText;

    [Header("Buttons")]
    public Button craftButton;
    public Button closeButton;
    public TextMeshProUGUI craftButtonText;

    [Header("Feedback")]
    public TextMeshProUGUI feedbackText;

    [Header("Inventory Preview (ซ้าย)")]
    [Tooltip("Parent สำหรับ spawn slot preview (ควรเป็น GridLayoutGroup)")]
    public Transform inventoryPreviewParent;
    [Tooltip("Prefab slot เล็กๆ — ต้องมี Image ชื่อ 'Icon' และ TMP ชื่อ 'Amount'")]
    public GameObject inventorySlotPreviewPrefab;
    [Tooltip("แสดง Hotbar ด้วยหรือเปล่า (ต่อท้าย Inventory)")]
    public bool showHotbarInPreview = true;

    [Header("Config")]
    public int workbenchLevel = 0;

    // Runtime
    CraftingRecipeSO selectedRecipe;
    List<GameObject> spawnedRecipeButtons = new List<GameObject>();
    List<GameObject> spawnedIngredientRows = new List<GameObject>();
    List<GameObject> spawnedPreviewSlots = new List<GameObject>();
    bool isOpen;

    void Awake()
    {
        Instance = this;
        // *** ไม่ซ่อน Panel ใน Awake เพราะ CraftingManager อาจยังไม่ได้ Awake ***
        // ย้ายไปทำใน Start() แทน เพื่อให้ทุก Awake() รันก่อน
    }

    void Start()
    {
        // ซ่อน Panel หลังจาก Awake() ทั้งหมดรันแล้ว (รวมถึง CraftingManager)
        // if (craftingPanel) craftingPanel.SetActive(false);

        if (craftButton) craftButton.onClick.AddListener(OnCraftPressed);
        if (closeButton) closeButton.onClick.AddListener(Close);
    }

    void Update()
    {
        // if (isOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    // ================================================================
    // Open / Close
    // ================================================================

    public void Open()
    {
        // if (isOpen) return;
        // isOpen = true;

        // if (craftingPanel) craftingPanel.SetActive(true);

        // // *** FIX: Unlock cursor เสมอเมื่อเปิด CraftingPanel ***
        // // ไม่ใส่เงื่อนไข เพราะถ้า cursor ยัง Lock อยู่จะคลิกอะไรไม่ได้เลย
        // Cursor.visible = true;
        // Cursor.lockState = CursorLockMode.None;

        // หยุดเวลา (optional — comment ออกถ้าไม่ต้องการ)
        // Time.timeScale = 0f;

        RefreshRecipeList();
        RefreshInventoryPreview();
        ClearDetail();
        ClearFeedback();
    }

    public void Close()
    {
        // if (!isOpen) return;
        // isOpen = false;

        // if (craftingPanel) craftingPanel.SetActive(false);

        // // Restore cursor — คืนค่าเฉพาะเมื่อไม่มี UI อื่นเปิดอยู่
        // bool anyUIOpen = (InventoryMainUI.Instance != null && InventoryMainUI.IsOpen);
        // if (!anyUIOpen)
        // {
        //     Cursor.visible = false;
        //     Cursor.lockState = CursorLockMode.Locked;
        // }

        // Time.timeScale = 1f;
    }

    // public void Toggle()
    // {
    //     if (isOpen) Close();
    //     else Open();
    // }

    // ================================================================
    // Recipe List
    // ================================================================

    void RefreshRecipeList()
    {
        // ลบเก่า
        // foreach (var obj in spawnedRecipeButtons) if (obj) Destroy(obj);
        // spawnedRecipeButtons.Clear();

        // if (CraftingManager.Instance == null) return;

        // var recipes = CraftingManager.Instance.GetAvailableRecipes(workbenchLevel);

        // foreach (var recipe in recipes)
        // {
        //     if (recipeButtonPrefab == null || recipeListParent == null) continue;

        //     var btn = Instantiate(recipeButtonPrefab, recipeListParent);
        //     spawnedRecipeButtons.Add(btn);

        //     // ตั้งค่า UI
        //     var label = btn.GetComponentInChildren<TextMeshProUGUI>();
        //     if (label) label.text = recipe.recipeName;

        //     var icon = btn.transform.Find("Icon")?.GetComponent<Image>();
        //     if (icon)
        //     {
        //         if (recipe.icon) icon.sprite = recipe.icon;
        //         else icon.color = Color.clear; // ซ่อน Icon ถ้าไม่มี Sprite
        //     }

        //     // Highlight สีตามว่าคราฟได้ไหม
        //     // var canCraft = CraftingManager.Instance.CanCraft(recipe) == CraftResult.Success;
        //     var bgImage = btn.GetComponent<Image>();
        //     if (bgImage) bgImage.color = canCraft ? new Color(0.8f, 1f, 0.8f) : new Color(1f, 0.85f, 0.85f);

        //     // Click → select recipe
        //     var captured = recipe;
        //     btn.GetComponent<Button>()?.onClick.AddListener(() => SelectRecipe(captured));
        // }
    }

    // ================================================================
    // Inventory Preview
    // ================================================================

    void RefreshInventoryPreview()
    {
        if (inventoryPreviewParent == null || inventorySlotPreviewPrefab == null) return;

        // ลบ slot เก่าออก
        foreach (var obj in spawnedPreviewSlots) if (obj) Destroy(obj);
        spawnedPreviewSlots.Clear();

        // --- Inventory Slots ---
        if (InventoryMainUI.Instance != null)
        {
            foreach (var slot in InventoryMainUI.Instance.slots)
            {
                SpawnPreviewSlot(slot?.item, slot?.amount ?? 0);
            }
        }

        // --- Hotbar Slots ---
        if (showHotbarInPreview && HotbarUI.Instance != null)
        {
            foreach (var slot in HotbarUI.Instance.slots)
            {
                if (slot == null) continue;
                SpawnPreviewSlot(slot.item, slot.amount);
            }
        }
    }

    void SpawnPreviewSlot(ItemSO item, int amount)
    {
        var obj = Instantiate(inventorySlotPreviewPrefab, inventoryPreviewParent);
        spawnedPreviewSlots.Add(obj);

        // Icon
        var icon = obj.transform.Find("Icon")?.GetComponent<UnityEngine.UI.Image>();
        if (icon)
        {
            if (item != null && item.icon)
            {
                icon.sprite = item.icon;
                icon.enabled = true;
            }
            else
            {
                icon.sprite = null;
                icon.enabled = false;
            }
        }

        // Amount text
        var amountTxt = obj.transform.Find("Amount")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (amountTxt)
        {
            amountTxt.text = (item != null && amount > 0) ? amount.ToString() : "";
        }

        // Slot ว่าง → ทำให้ดูจางๆ
        var bg = obj.GetComponent<UnityEngine.UI.Image>();
        if (bg)
        {
            bg.color = (item != null) ? Color.white : new Color(1f, 1f, 1f, 0.3f);
        }
    }

    // ================================================================
    // Recipe Detail
    // ================================================================

    void SelectRecipe(CraftingRecipeSO recipe)
    {
        selectedRecipe = recipe;
        ClearFeedback();

        if (selectedIcon) selectedIcon.sprite = recipe.icon;
        if (selectedNameText) selectedNameText.text = recipe.recipeName;
        if (selectedDescText) selectedDescText.text = recipe.description;

        // Ingredients
        foreach (var obj in spawnedIngredientRows) if (obj) Destroy(obj);
        spawnedIngredientRows.Clear();

        // if (recipe.ingredients != null && ingredientRowPrefab && ingredientListParent)
        // {
        //     foreach (var ing in recipe.ingredients)
        //     {
        //         if (ing.item == null) continue;

        //         var row = Instantiate(ingredientRowPrefab, ingredientListParent);
        //         spawnedIngredientRows.Add(row);

        //         var nameTxt = row.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        //         var amountTxt = row.transform.Find("Amount")?.GetComponent<TextMeshProUGUI>();
        //         var iconImg = row.transform.Find("Icon")?.GetComponent<Image>();

        //         int have = CraftingManager.Instance.CountItem(ing.item);
        //         bool enough = have >= ing.amount;

        //         if (nameTxt) nameTxt.text = ing.item.itemName;
        //         if (amountTxt)
        //         {
        //             amountTxt.text = $"{have}/{ing.amount}";
        //             amountTxt.color = enough ? Color.green : Color.red;
        //         }
        //         if (iconImg && ing.item.icon) iconImg.sprite = ing.item.icon;
        //     }
        // }

        // // Result
        // if (resultText)
        //     resultText.text = $"ผลลัพธ์: {recipe.resultItem.itemName} x{recipe.resultAmount}";

        // // Craft Button
        // var canCraft = CraftingManager.Instance.CanCraft(recipe);
        // if (craftButton) craftButton.interactable = (canCraft == CraftResult.Success);
        // if (craftButtonText)
        // {
        //     switch (canCraft)
        //     {
        //         case CraftResult.Success: craftButtonText.text = "คราฟ!"; break;
        //         case CraftResult.NotEnoughMaterials: craftButtonText.text = "วัตถุดิบไม่พอ"; break;
        //         case CraftResult.InventoryFull: craftButtonText.text = "Inventory เต็ม"; break;
        //         case CraftResult.NotEnoughEnergy: craftButtonText.text = "พลังงานไม่พอ"; break;
        //         default: craftButtonText.text = "ไม่สามารถคราฟได้"; break;
        //     }
        // }
    }

    void ClearDetail()
    {
        selectedRecipe = null;
        if (selectedIcon) selectedIcon.sprite = null;
        if (selectedNameText) selectedNameText.text = "";
        if (selectedDescText) selectedDescText.text = "เลือกสูตรจากรายการ";
        if (resultText) resultText.text = "";
        if (craftButton) craftButton.interactable = false;
        if (craftButtonText) craftButtonText.text = "เลือกสูตร";

        foreach (var obj in spawnedIngredientRows) if (obj) Destroy(obj);
        spawnedIngredientRows.Clear();
    }

    // ================================================================
    // Craft Action
    // ================================================================

    void OnCraftPressed()
    {
        if (selectedRecipe == null) return;
        if (CraftingManager.Instance == null) return;

        // var result = CraftingManager.Instance.Craft(selectedRecipe);

        // switch (result)
        // {
        //     case CraftResult.Success:
        //         ShowFeedback($"คราฟ {selectedRecipe.resultItem.itemName} สำเร็จ!", Color.green);
        //         // Refresh ทั้งหมดเพื่ออัพเดท stock
        //         RefreshRecipeList();
        //         RefreshInventoryPreview();
        //         SelectRecipe(selectedRecipe);
        //         break;
        //     case CraftResult.NotEnoughMaterials:
        //         ShowFeedback("วัตถุดิบไม่พอ!", Color.red);
        //         break;
        //     case CraftResult.InventoryFull:
        //         ShowFeedback("Inventory เต็ม!", Color.red);
        //         break;
        //     default:
        //         ShowFeedback("ไม่สามารถคราฟได้", Color.red);
        //         break;
        // }
    }

    // ================================================================
    // Feedback
    // ================================================================

    void ShowFeedback(string msg, Color color)
    {
        if (feedbackText)
        {
            feedbackText.text = msg;
            feedbackText.color = color;
        }
    }

    void ClearFeedback()
    {
        if (feedbackText) feedbackText.text = "";
    }
}
