using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// [UPGRADED] Shop UI — รองรับ:
/// - หลายร้านค้า (ShopDefinition per NPC)
/// - ราคาตลาด Supply/Demand (ผ่าน MarketPriceSystem)
/// - ระบบ Restock + ของหมด (Stock)
/// - ของพิเศษเฉพาะบางวัน (DayAvailability)
/// - แสดง NPC Portrait + ชื่อร้าน
/// - SFX
/// - Tab Category
/// </summary>
public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    [Header("UI Root")]
    public GameObject panel;
    public Transform listParent;
    public ShopItemView itemPrefab;

    [Header("Tabs (Categories)")]
    public Transform tabsParent;
    public ShopTabButton tabButtonPrefab;

    [Header("Shop Header")]
    public TextMeshProUGUI headerLabel;
    public TextMeshProUGUI merchantNameLabel;
    public Image merchantPortraitImage;

    [Header("Player Freeze while open")]
    public Transform player;
    public Behaviour[] toDisable;
    public bool unlockCursorOnOpen = true;
    public bool pauseWithTimeScale = false;

    [Header("Feedback")]
    public TextMeshProUGUI feedbackText;

    // ---------- SFX ----------
    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip buySuccessSfx;
    public AudioClip buyFailSfx;
    public AudioClip soldOutSfx;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    // ---------- Custom Tabs ----------
    [System.Serializable]
    public struct TabSpec
    {
        public ShopCategory category;
        public string labelOverride;
        public bool hideIfEmpty;
    }

    [Header("Tabs - Custom Mode")]
    public bool useCustomTabs = false;
    public List<TabSpec> customTabs = new List<TabSpec>()
    {
        new TabSpec{ category = ShopCategory.All,    labelOverride = "ทั้งหมด",  hideIfEmpty = false },
        new TabSpec{ category = ShopCategory.Seeds,  labelOverride = "เมล็ด",    hideIfEmpty = true  },
        new TabSpec{ category = ShopCategory.Food,   labelOverride = "อาหาร",    hideIfEmpty = true  },
        new TabSpec{ category = ShopCategory.Tools,  labelOverride = "เครื่องมือ", hideIfEmpty = true  },
        new TabSpec{ category = ShopCategory.Materials, labelOverride = "วัตถุดิบ", hideIfEmpty = true },
        new TabSpec{ category = ShopCategory.Others, labelOverride = "อื่น ๆ",   hideIfEmpty = true  },
    };

    // ---------- Private ----------
    ShopDefinition _current;
    readonly List<ShopItemView> _spawnedItems = new();
    readonly List<ShopTabButton> _spawnedTabs = new();
    ShopCategory _currentCategory = ShopCategory.All;

    bool _isOpen;
    float _prevTimeScale = 1f;
    bool[] _wasEnabled;

    void Awake()
    {
        Instance = this;
        if (panel) panel.SetActive(false);
        EnsureSfxSource();
    }

    void EnsureSfxSource()
    {
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (!sfxSource) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }
    }

    void PlaySfx(AudioClip clip)
    {
        if (!clip) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    // ================================================================
    // Open / Close
    // ================================================================

    public void Open(ShopDefinition def)
    {
        if (_isOpen) return;
        _isOpen = true;
        _current = def;
        _currentCategory = ShopCategory.All;

        if (panel) panel.SetActive(true);

        // Header
        if (headerLabel) headerLabel.text = def != null ? def.shopName : "Shop";
        if (merchantNameLabel)
            merchantNameLabel.text = (def != null && !string.IsNullOrEmpty(def.merchantName)) ? def.merchantName : "";
        if (merchantPortraitImage)
        {
            if (def != null && def.merchantPortrait != null)
            {
                merchantPortraitImage.sprite = def.merchantPortrait;
                merchantPortraitImage.enabled = true;
            }
            else
            {
                merchantPortraitImage.enabled = false;
            }
        }

        BuildTabs(def);
        BuildList(def);
        FreezeControls(true);
        ClearFeedback();

        if (unlockCursorOnOpen)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;

        if (panel) panel.SetActive(false);
        ClearList();
        ClearTabs();
        FreezeControls(false);

        if (unlockCursorOnOpen)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void Update()
    {
        if (_isOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    // ================================================================
    // Tabs
    // ================================================================

    void BuildTabs(ShopDefinition def)
    {
        ClearTabs();
        if (tabsParent == null || tabButtonPrefab == null) return;

        if (useCustomTabs && customTabs != null && customTabs.Count > 0)
        {
            foreach (var spec in customTabs)
            {
                if (spec.category != ShopCategory.All && spec.hideIfEmpty)
                {
                    if (!HasItemsInCategory(def, spec.category))
                        continue;
                }
                var btn = Instantiate(tabButtonPrefab, tabsParent);
                if (!string.IsNullOrWhiteSpace(spec.labelOverride) && btn.label)
                    btn.label.text = spec.labelOverride;
                btn.Setup(spec.category, SetCategory);
                btn.SetActiveVisual(spec.category == _currentCategory);
                _spawnedTabs.Add(btn);
            }
        }
        else
        {
            var cats = new HashSet<ShopCategory> { ShopCategory.All };
            if (def != null)
                foreach (var e in def.items)
                    if (e != null) cats.Add(e.category);

            var ordered = new List<ShopCategory>(cats);
            ordered.Sort((a, b) =>
            {
                if (a == ShopCategory.All) return -1;
                if (b == ShopCategory.All) return 1;
                return a.CompareTo(b);
            });

            foreach (var c in ordered)
            {
                var btn = Instantiate(tabButtonPrefab, tabsParent);
                btn.Setup(c, SetCategory);
                btn.SetActiveVisual(c == _currentCategory);
                _spawnedTabs.Add(btn);
            }
        }

        var rt = tabsParent as RectTransform;
        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    bool HasItemsInCategory(ShopDefinition def, ShopCategory cat)
    {
        if (def == null) return false;
        if (cat == ShopCategory.All) return def.items != null && def.items.Count > 0;
        foreach (var e in def.items)
            if (e != null && e.item != null && e.category == cat) return true;
        return false;
    }

    void ClearTabs()
    {
        foreach (var t in _spawnedTabs) if (t) Destroy(t.gameObject);
        _spawnedTabs.Clear();
    }

    void SetCategory(ShopCategory category)
    {
        _currentCategory = category;
        foreach (var t in _spawnedTabs)
            if (t) t.SetActiveVisual(t.category == _currentCategory);
        BuildList(_current);
    }

    // ================================================================
    // Item List
    // ================================================================

    void BuildList(ShopDefinition def)
    {
        ClearList();
        if (def == null || itemPrefab == null || listParent == null) return;

        IEnumerable<ShopDefinition.Entry> list = def.items;
        if (_currentCategory != ShopCategory.All)
            list = list.Where(e => e != null && e.category == _currentCategory);

        foreach (var e in list)
        {
            if (e == null || e.item == null) continue;

            // เช็คของหมดหรือเปล่า
            bool soldOut = (e.maxStock > 0 && e.currentStock <= 0);

            var view = Instantiate(itemPrefab, listParent);
            view.Setup(e.item, e.price, e.maxPerClick, OnBuyRequest, e.currentStock, soldOut);
            _spawnedItems.Add(view);
        }
    }

    void ClearList()
    {
        foreach (var v in _spawnedItems) if (v) Destroy(v.gameObject);
        _spawnedItems.Clear();
    }

    // ================================================================
    // Controls Freeze
    // ================================================================

    void FreezeControls(bool on)
    {
        if (toDisable == null || toDisable.Length == 0) return;

        if (on)
        {
            if (_wasEnabled == null || _wasEnabled.Length != toDisable.Length)
                _wasEnabled = new bool[toDisable.Length];
            for (int i = 0; i < toDisable.Length; i++)
            {
                var b = toDisable[i]; if (!b) continue;
                _wasEnabled[i] = b.enabled;
                b.enabled = false;
            }
            if (pauseWithTimeScale) { _prevTimeScale = Time.timeScale; Time.timeScale = 0f; }
        }
        else
        {
            for (int i = 0; i < toDisable.Length; i++)
            {
                var b = toDisable[i]; if (!b) continue;
                bool back = (_wasEnabled != null && i < _wasEnabled.Length) ? _wasEnabled[i] : true;
                b.enabled = back;
            }
            if (pauseWithTimeScale) Time.timeScale = _prevTimeScale;
        }
    }

    // ================================================================
    // Purchase
    // ================================================================

    void OnBuyRequest(ItemSO item, int priceEach, int amount)
    {
        if (item == null || amount <= 0) return;

        // หา Entry ใน ShopDefinition เพื่อลดสต๊อก
        ShopDefinition.Entry entry = null;
        if (_current != null)
        {
            foreach (var e in _current.items)
                if (e != null && e.item == item) { entry = e; break; }
        }

        // เช็คสต๊อก
        if (entry != null && entry.maxStock > 0)
        {
            if (entry.currentStock <= 0)
            {
                ShowFeedback("ของหมดแล้ว!", Color.red);
                PlaySfx(soldOutSfx != null ? soldOutSfx : buyFailSfx);
                return;
            }
            amount = Mathf.Min(amount, entry.currentStock);
        }

        int total = Mathf.Max(0, priceEach) * amount;

        var wallet = PlayerWallet.Instance;
        if (wallet == null) { PlaySfx(buyFailSfx); return; }

        if (!wallet.TrySpend(total))
        {
            ShowFeedback("เงินไม่พอ!", Color.red);
            PlaySfx(buyFailSfx);
            return;
        }

        bool added = InventoryMainUI.Instance != null && InventoryMainUI.Instance.AddItemToInventory(item, amount);
        if (!added)
        {
            wallet.Add(total);
            ShowFeedback("Inventory เต็ม!", Color.red);
            PlaySfx(buyFailSfx);
            return;
        }

        // ลดสต๊อก
        if (entry != null && entry.maxStock > 0)
        {
            entry.currentStock -= amount;
        }

        ShowFeedback($"ซื้อ {item.itemName} x{amount} — ¥{total:N0}", new Color(0.2f, 0.8f, 0.2f));
        PlaySfx(buySuccessSfx);

        // Refresh list เพื่ออัพเดทสต๊อก
        BuildList(_current);
    }

    // ================================================================
    // Feedback
    // ================================================================

    void ShowFeedback(string msg, Color c)
    {
        if (feedbackText) { feedbackText.text = msg; feedbackText.color = c; }
    }

    void ClearFeedback()
    {
        if (feedbackText) feedbackText.text = "";
    }
}
