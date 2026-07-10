using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// แถบหัวใจที่ใช้ทั้งใน list ฝั่งซ้าย (เล็ก) และ panel รายละเอียดฝั่งขวา (ใหญ่) —
/// สร้างไอคอนหัวใจตามจำนวน max แล้วเปลี่ยนสีดวงที่ "ได้แล้ว" เท่านั้น ไม่ Destroy/
/// Instantiate ใหม่ทุกครั้งที่ level เปลี่ยน (แค่ไล่สี)
/// </summary>
public class HeartMeterUI : MonoBehaviour
{
    [SerializeField] private Image heartIconPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private Color filledColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.25f);

    private readonly List<Image> _icons = new();

    public void SetHearts(int maxHearts, int currentHearts)
    {
        while (_icons.Count < maxHearts)
            _icons.Add(Instantiate(heartIconPrefab, container));

        for (int i = 0; i < _icons.Count; i++)
        {
            bool shouldShow = i < maxHearts;
            _icons[i].gameObject.SetActive(shouldShow);
            if (shouldShow) _icons[i].color = i < currentHearts ? filledColor : emptyColor;
        }
    }
}
