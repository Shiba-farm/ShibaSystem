using System.Collections.Generic;

/// <summary>Registry กลางของหมุดทั้งหมดบนแผนที่ — รูปแบบเดียวกับ InventoryUIRegistry</summary>
public static class MapMarkerRegistry
{
    private static readonly List<IMapMarkerSource> _sources = new();

    public static void Register(IMapMarkerSource source)
    {
        if (!_sources.Contains(source)) _sources.Add(source);
    }

    public static void Unregister(IMapMarkerSource source) => _sources.Remove(source);

    public static IReadOnlyList<IMapMarkerSource> Sources => _sources;
}
