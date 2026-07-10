using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window that scans every ItemSO asset in the project and reports:
///   • Type / category breakdown
///   • ID range, gaps, and next suggested ID
///   • Data-quality issues (duplicates, unset IDs, missing fields)
///   • Full sortable item list with one-click ping
///
/// Open via:  Window ▸ UnityShiba ▸ Item Database Inspector
/// </summary>
public class ItemDatabaseInspector : EditorWindow
{
    // ── Menu entry ─────────────────────────────────────────────────────────────

    [MenuItem("Window/UnityShiba/Item Database Inspector")]
    public static void Open() => GetWindow<ItemDatabaseInspector>("Item DB Inspector");

    // ── Scan results ───────────────────────────────────────────────────────────

    private List<ItemSO> _items       = new();
    private string       _lastScan    = "not scanned yet";
    private int          _total;

    // Breakdown tables
    private Dictionary<string, int>        _byType     = new();
    private Dictionary<ItemCategory, int>  _byCategory = new();
    private int _sellableCount;

    // ID stats
    private int        _minId;
    private int        _maxId;
    private List<int>  _availableIds  = new();
    private int        _nextSuggestedId;

    // Quality issues  (list = clickable items)
    private List<(int id, List<ItemSO> dupes)> _dupIds       = new();
    private List<ItemSO>                        _zeroIdItems  = new();
    private List<(string name, List<ItemSO>)>   _dupNames     = new();
    private List<ItemSO>                        _missingName  = new();
    private List<ItemSO>                        _missingIcon  = new();
    private List<ItemSO>                        _missingWorld = new();
    private List<ItemSO>                        _missingEquip = new();

    // UI state
    private Vector2 _scroll;
    private bool    _showOverview  = true;
    private bool    _showIdStats   = true;
    private bool    _showQuality   = true;
    private bool    _showAllItems  = false;

    // Styles (initialised once inside OnGUI)
    private GUIStyle _sectionHeader;
    private GUIStyle _okStyle;
    private GUIStyle _warnStyle;
    private GUIStyle _errorStyle;
    private GUIStyle _monoLabel;
    private bool     _stylesReady;

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    private void OnEnable() => Scan();

    private void OnGUI()
    {
        EnsureStyles();
        DrawToolbar();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.Space(4);

        DrawOverview();
        DrawIdStats();
        DrawQuality();
        DrawAllItems();

        EditorGUILayout.Space(8);
        EditorGUILayout.EndScrollView();
    }

    // ── Toolbar ────────────────────────────────────────────────────────────────

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label($"  Last scan: {_lastScan}", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            Scan();

        EditorGUILayout.EndHorizontal();
    }

    // ── Overview section ───────────────────────────────────────────────────────

    private void DrawOverview()
    {
        _showOverview = SectionHeader("Overview", _showOverview);
        if (!_showOverview) return;

        EditorGUI.indentLevel++;

        LabelRow("Total items", _total.ToString(), EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("By type", EditorStyles.boldLabel);
        foreach (var kvp in _byType.OrderByDescending(x => x.Value))
            LabelRow("  " + kvp.Key, kvp.Value.ToString());

        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("By category", EditorStyles.boldLabel);
        foreach (var kvp in _byCategory.OrderByDescending(x => x.Value))
            LabelRow("  " + kvp.Key, kvp.Value.ToString());

        EditorGUILayout.Space(4);
        LabelRow("Sellable", $"{_sellableCount} / {_total}");
        LabelRow("Not sellable", (_total - _sellableCount).ToString());

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(6);
    }

    // ── ID stats section ───────────────────────────────────────────────────────

    private void DrawIdStats()
    {
        _showIdStats = SectionHeader("ID Stats", _showIdStats);
        if (!_showIdStats) return;

        EditorGUI.indentLevel++;

        int usedCount = _total - _zeroIdItems.Count;
        LabelRow("Min ID",  _items.Count > 0 ? _minId.ToString() : "—");
        LabelRow("Max ID",  _items.Count > 0 ? _maxId.ToString() : "—");
        LabelRow("Used IDs", usedCount.ToString());

        EditorGUILayout.Space(2);

        if (_availableIds.Count == 0)
        {
            LabelRow("Available IDs (gaps)", "none");
        }
        else
        {
            string ids = _availableIds.Count <= 25
                ? string.Join(", ", _availableIds)
                : string.Join(", ", _availableIds.Take(25)) + $"  … (+{_availableIds.Count - 25} more)";
            LabelRow($"Available IDs ({_availableIds.Count})", ids);
        }

        EditorGUILayout.Space(2);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Next suggested ID",
            _nextSuggestedId.ToString(), EditorStyles.boldLabel);
        if (GUILayout.Button("Copy", GUILayout.Width(48)))
            GUIUtility.systemCopyBuffer = _nextSuggestedId.ToString();
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(6);
    }

    // ── Data quality section ───────────────────────────────────────────────────

    private void DrawQuality()
    {
        _showQuality = SectionHeader("Data Quality", _showQuality);
        if (!_showQuality) return;

        EditorGUI.indentLevel++;

        DrawIssueRow("Duplicate IDs",        _dupIds.SelectMany(d => d.dupes).ToList(),   isError: true);
        DrawIssueRow("ID = 0 (unset)",        _zeroIdItems,                                isError: true);
        DrawIssueRow("Duplicate names",       _dupNames.SelectMany(d => d.Item2).ToList(), isError: false);
        DrawIssueRow("Missing itemName",      _missingName,                                isError: false);
        DrawIssueRow("Missing icon",          _missingIcon,                                isError: false);
        DrawIssueRow("Missing worldPrefab",   _missingWorld,                               isError: false);
        DrawIssueRow("Missing equipPrefab",   _missingEquip,                               isError: false);

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(6);
    }

    private void DrawIssueRow(string label, List<ItemSO> items, bool isError)
    {
        int count = items.Count;
        GUIStyle style = count == 0 ? _okStyle : (isError ? _errorStyle : _warnStyle);
        string badge = count == 0 ? "✓" : (isError ? "✗" : "!");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{badge}  {label}", count == 0 ? "OK" : count.ToString(), style);
        EditorGUILayout.EndHorizontal();

        if (count > 0)
        {
            EditorGUI.indentLevel++;
            foreach (var item in items)
            {
                string display = string.IsNullOrEmpty(item.itemName)
                    ? $"[{item.name}]  (ID: {item.itemID})"
                    : $"{item.itemName}  (ID: {item.itemID})";

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUI.indentLevel * 15f);
                if (GUILayout.Button(display, EditorStyles.linkLabel))
                {
                    EditorGUIUtility.PingObject(item);
                    Selection.activeObject = item;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }
    }

    // ── All items list ─────────────────────────────────────────────────────────

    private void DrawAllItems()
    {
        _showAllItems = SectionHeader($"All Items  ({_total})", _showAllItems);
        if (!_showAllItems) return;

        EditorGUI.indentLevel++;

        // Column header
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(EditorGUI.indentLevel * 15f);
        EditorGUILayout.LabelField("ID",       GUILayout.Width(40));
        EditorGUILayout.LabelField("Name",     GUILayout.Width(160));
        EditorGUILayout.LabelField("Type",     GUILayout.Width(110));
        EditorGUILayout.LabelField("Category", GUILayout.Width(90));
        EditorGUILayout.LabelField("Sell",     GUILayout.Width(36));
        EditorGUILayout.EndHorizontal();

        Rect lineRect = GUILayoutUtility.GetRect(0, 1);
        EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));

        foreach (var item in _items.OrderBy(i => i.itemID))
        {
            bool hasProblem = item.itemID == 0
                           || string.IsNullOrEmpty(item.itemName)
                           || item.icon == null;

            if (hasProblem)
                GUI.color = new Color(1f, 0.85f, 0.5f);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15f);

            EditorGUILayout.LabelField(item.itemID.ToString(), _monoLabel, GUILayout.Width(40));

            string displayName = string.IsNullOrEmpty(item.itemName) ? "(unnamed)" : item.itemName;
            if (GUILayout.Button(displayName, EditorStyles.linkLabel, GUILayout.Width(160)))
            {
                EditorGUIUtility.PingObject(item);
                Selection.activeObject = item;
            }

            string shortType = item.GetType().Name.Replace("ItemSO", "").Replace("SO", "");
            EditorGUILayout.LabelField(shortType,           GUILayout.Width(110));
            EditorGUILayout.LabelField(item.category.ToString(), GUILayout.Width(90));
            EditorGUILayout.LabelField(item.sellable ? "Y" : "—", GUILayout.Width(36));

            EditorGUILayout.EndHorizontal();

            GUI.color = Color.white;
        }

        EditorGUI.indentLevel--;
    }

    // ── Scan ───────────────────────────────────────────────────────────────────

    private void Scan()
    {
        _items.Clear();
        _byType.Clear();
        _byCategory.Clear();
        _dupIds.Clear();
        _zeroIdItems.Clear();
        _dupNames.Clear();
        _missingName.Clear();
        _missingIcon.Clear();
        _missingWorld.Clear();
        _missingEquip.Clear();
        _availableIds.Clear();

        // ── Load every ItemSO in the project ──────────────────────────────────
        foreach (var guid in AssetDatabase.FindAssets("t:ItemSO"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var item    = AssetDatabase.LoadAssetAtPath<ItemSO>(path);
            if (item != null) _items.Add(item);
        }

        _total = _items.Count;
        if (_total == 0) { _lastScan = Now(); Repaint(); return; }

        // ── Type / category breakdown ─────────────────────────────────────────
        foreach (var item in _items)
        {
            string t = item.GetType().Name;
            _byType[t] = _byType.TryGetValue(t, out int tc) ? tc + 1 : 1;

            _byCategory[item.category] =
                _byCategory.TryGetValue(item.category, out int cc) ? cc + 1 : 1;
        }

        _sellableCount = _items.Count(i => i.sellable);

        // ── ID analysis ───────────────────────────────────────────────────────
        var withId = _items.Where(i => i.itemID > 0).ToList();
        if (withId.Count > 0)
        {
            _minId = withId.Min(i => i.itemID);
            _maxId = withId.Max(i => i.itemID);

            var usedSet = new HashSet<int>(withId.Select(i => i.itemID));
            for (int id = 1; id <= _maxId; id++)
                if (!usedSet.Contains(id))
                    _availableIds.Add(id);

            // Suggest lowest gap first, otherwise maxId+1
            _nextSuggestedId = _availableIds.Count > 0 ? _availableIds[0] : _maxId + 1;

            // Duplicate IDs
            foreach (var grp in withId.GroupBy(i => i.itemID).Where(g => g.Count() > 1))
                _dupIds.Add((grp.Key, grp.ToList()));
        }
        else
        {
            _nextSuggestedId = 1;
        }

        // ── Data quality ──────────────────────────────────────────────────────
        _zeroIdItems  = _items.Where(i => i.itemID == 0).ToList();
        _missingName  = _items.Where(i => string.IsNullOrEmpty(i.itemName)).ToList();
        _missingIcon  = _items.Where(i => i.icon == null).ToList();
        _missingWorld = _items.Where(i => i.worldItemPrefab == null).ToList();
        _missingEquip = _items.Where(i => i.equipmentPrefab == null).ToList();

        foreach (var grp in _items
            .Where(i => !string.IsNullOrEmpty(i.itemName))
            .GroupBy(i => i.itemName)
            .Where(g => g.Count() > 1))
        {
            _dupNames.Add((grp.Key, grp.ToList()));
        }

        _lastScan = Now();
        Repaint();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string Now() => DateTime.Now.ToString("HH:mm:ss");

    private static void LabelRow(string label, string value, GUIStyle valueStyle = null)
    {
        if (valueStyle != null)
            EditorGUILayout.LabelField(label, value, valueStyle);
        else
            EditorGUILayout.LabelField(label, value);
    }

    private bool SectionHeader(string title, bool open)
    {
        EditorGUILayout.Space(2);
        bool next = EditorGUILayout.Foldout(open, title, true, _sectionHeader);
        Rect r = GUILayoutUtility.GetRect(0, 1);
        EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        EditorGUILayout.Space(2);
        return next;
    }

    private void EnsureStyles()
    {
        if (_stylesReady) return;

        _sectionHeader = new GUIStyle(EditorStyles.foldout)
        {
            fontSize  = 12,
            fontStyle = FontStyle.Bold,
        };

        _okStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = EditorGUIUtility.isProSkin
                ? new Color(0.45f, 0.9f, 0.45f)
                : new Color(0.1f, 0.55f, 0.1f) }
        };

        _warnStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 0.78f, 0.2f)
                : new Color(0.7f, 0.45f, 0f) }
        };

        _errorStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 0.38f, 0.38f)
                : new Color(0.8f, 0.1f, 0.1f) }
        };

        _monoLabel = new GUIStyle(EditorStyles.label)
        {
            font = EditorStyles.boldLabel.font,
            normal = { textColor = EditorStyles.label.normal.textColor }
        };

        _stylesReady = true;
    }
}
