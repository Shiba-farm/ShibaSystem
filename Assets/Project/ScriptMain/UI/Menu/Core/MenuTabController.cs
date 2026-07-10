using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tab Controller — รับผิดชอบ "สลับแท็บภายในหน้าต่างเมนูเดียวกัน" เท่านั้น
/// (เปิด/ปิดหน้าต่างทั้งบานเป็นหน้าที่ของ MenuWindowController / InGameUIManager)
///
/// - Lazy init: แท็บที่ยังไม่เคยถูกเปิดจะไม่ InitializeTab() จนกว่าจะถูกคลิกครั้งแรก
///   (ประหยัด — ไม่ผูก signal ของทุกแท็บตั้งแต่เกมเริ่ม)
/// - แต่ละแท็บคุยผ่าน IMenuTabView เท่านั้น — เพิ่มแท็บใหม่ในอนาคตไม่ต้องแก้ class นี้
///   แค่เพิ่ม element ใน list "tabs" ใน Inspector
/// </summary>
public class MenuTabController : MonoBehaviour
{
    [Serializable]
    public struct MenuTabRegistration
    {
        public MenuTabId tabId;
        public MenuTabButtonUI button;     // ปุ่มแท็บ (เลข 1-6)
        public GameObject viewObject;      // GameObject ที่มี component implement IMenuTabView
    }

    [SerializeField] private List<MenuTabRegistration> tabs;
    [SerializeField] private MenuTabId defaultTab = MenuTabId.Inventory;

    public event Action<MenuTabId> OnTabChanged;
    public MenuTabId CurrentTab { get; private set; }

    private readonly Dictionary<MenuTabId, MenuTabRegistration> _lookup = new();
    private bool _wired;

    private void Awake() => BuildLookupAndWireButtons();

    private void BuildLookupAndWireButtons()
    {
        if (_wired) return;
        _wired = true;

        foreach (var entry in tabs)
        {
            _lookup[entry.tabId] = entry;
            if (entry.button != null)
                entry.button.OnClicked += ShowTab;

            if (entry.viewObject != null)
                entry.viewObject.SetActive(false);
        }
    }

    /// <summary>เรียกจาก MenuWindowController ทุกครั้งที่หน้าต่างถูกเปิด</summary>
    public void ShowDefaultOrLastTab()
    {
        BuildLookupAndWireButtons();
        ShowTab(_lookup.ContainsKey(CurrentTab) ? CurrentTab : defaultTab);
    }

    public void ShowTab(MenuTabId tabId)
    {
        BuildLookupAndWireButtons();
        if (!_lookup.TryGetValue(tabId, out var target))
        {
            Debug.LogWarning($"[MenuTabController] Tab '{tabId}' ไม่ได้ลงทะเบียนใน Inspector");
            return;
        }

        // ปิดแท็บก่อนหน้า
        if (_lookup.TryGetValue(CurrentTab, out var previous) && previous.tabId != tabId)
        {
            previous.viewObject?.SetActive(false);
            previous.button?.SetActive(false);
            (previous.viewObject?.GetComponent<IMenuTabView>())?.OnTabHidden();
        }

        target.viewObject?.SetActive(true);
        target.button?.SetActive(true);
        CurrentTab = tabId;

        var view = target.viewObject?.GetComponent<IMenuTabView>();
        if (view != null)
        {
            if (!view.IsInitialized) view.InitializeTab();
            view.OnTabShown();
        }

        OnTabChanged?.Invoke(tabId);
    }
}
