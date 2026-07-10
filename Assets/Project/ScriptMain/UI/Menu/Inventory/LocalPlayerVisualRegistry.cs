using UnityEngine;

/// <summary>
/// จุดเดียวที่เก็บ "โมเดลภายนอกของผู้เล่น local" ไว้ให้ UI preview เอาไปโคลนแสดงผล
/// ไม่เก็บ reference ของผู้เล่นคนอื่นเด็ดขาด — LocalPlayerVisualMarker จะ Register
/// ที่นี่ก็ต่อเมื่อ IsOwner เท่านั้น (ดูคอมเมนต์ใน LocalPlayerVisualMarker.cs)
/// </summary>
public static class LocalPlayerVisualRegistry
{
    public static GameObject VisualRoot { get; private set; }

    public static void Register(GameObject root) => VisualRoot = root;

    public static void Unregister(GameObject root)
    {
        if (VisualRoot == root) VisualRoot = null;
    }
}
