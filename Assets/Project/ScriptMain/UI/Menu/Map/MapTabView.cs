using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// แท็บ Map — วาดรูปแผนที่เกาะ + หมุดทั้งหมดจาก MapMarkerRegistry (ผู้เล่น/NPC/
/// เควส/หมุดกำหนดเอง) ตำแหน่งหมุดอัปเดตทุกเฟรมที่แท็บเปิดอยู่เท่านั้น (Update จะ
/// ไม่รันเองเมื่อ GameObject ถูกปิดโดย MenuTabController ตอนสลับแท็บ — ประหยัด)
///
/// เพิ่ม marker ประเภทใหม่ในอนาคต = implement IMapMarkerSource ใหม่แล้ว Register
/// เข้า MapMarkerRegistry ไม่ต้องแก้ไฟล์นี้เลย
/// </summary>
public class MapTabView : MonoBehaviour, IMenuTabView
{
    [SerializeField] private MapBoundsSO mapBounds;
    [SerializeField] private RectTransform mapContent; // รูปแผนที่ — ขนาดจริงของมันคือพื้นที่ที่หมุดจะ map ลงไป
    [SerializeField] private MapMarkerUI markerPrefab;
    [SerializeField] private Transform markerContainer;

    public MenuTabId TabId => MenuTabId.Map;
    public bool IsInitialized { get; private set; }

    private readonly Dictionary<IMapMarkerSource, MapMarkerUI> _activeMarkers = new();

    public void InitializeTab() => IsInitialized = true;

    public void OnTabShown() { }
    public void OnTabHidden() { }

    private void Update()
    {
        if (mapBounds == null || mapContent == null) return;

        var seen = new HashSet<IMapMarkerSource>();

        foreach (var source in MapMarkerRegistry.Sources)
        {
            if (source == null || source.MarkerTransform == null || !source.IsMarkerVisible) continue;

            if (!_activeMarkers.TryGetValue(source, out var markerUI))
            {
                markerUI = Instantiate(markerPrefab, markerContainer);
                markerUI.Setup(source);
                _activeMarkers[source] = markerUI;
            }

            Vector2 uv = mapBounds.WorldToUV(source.MarkerTransform.position);
            markerUI.SetNormalizedPosition(uv, mapContent.rect.size);
            seen.Add(source);
        }

        // ลบ marker ของ source ที่หายไปแล้ว (เช่น NPC ถูก despawn)
        List<IMapMarkerSource> toRemove = null;
        foreach (var kvp in _activeMarkers)
        {
            if (seen.Contains(kvp.Key)) continue;
            toRemove ??= new List<IMapMarkerSource>();
            toRemove.Add(kvp.Key);
        }
        if (toRemove != null)
        {
            foreach (var key in toRemove)
            {
                if (_activeMarkers[key] != null) Destroy(_activeMarkers[key].gameObject);
                _activeMarkers.Remove(key);
            }
        }
    }
}
