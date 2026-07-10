using UnityEngine;

/// <summary>
/// กำหนดขอบเขตโลก (world space, แกน X/Z) ที่ตรงกับรูปแผนที่ — ใช้แปลงตำแหน่งโลก
/// เป็นตำแหน่งบนรูปแผนที่ (UV 0-1) ปรับแค่ asset นี้ถ้าขนาดเกาะเปลี่ยน ไม่ต้องแก้โค้ด
/// </summary>
[CreateAssetMenu(menuName = "ShibaFarm/Map/Map Bounds")]
public class MapBoundsSO : ScriptableObject
{
    [Tooltip("มุมโลก (X,Z) ที่ตรงกับมุมล่างซ้ายของรูปแผนที่")]
    public Vector2 worldMin = new(-100, -100);
    [Tooltip("มุมโลก (X,Z) ที่ตรงกับมุมบนขวาของรูปแผนที่")]
    public Vector2 worldMax = new(100, 100);

    /// <summary>แปลงตำแหน่งโลกเป็น UV (0,0)=ล่างซ้าย, (1,1)=บนขวา ของรูปแผนที่</summary>
    public Vector2 WorldToUV(Vector3 worldPosition)
    {
        float u = Mathf.InverseLerp(worldMin.x, worldMax.x, worldPosition.x);
        float v = Mathf.InverseLerp(worldMin.y, worldMax.y, worldPosition.z);
        return new Vector2(u, v);
    }
}
