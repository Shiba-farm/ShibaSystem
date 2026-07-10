using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic object pool สำหรับ UI list/grid item (Quest row, NPC row, Skill row,
/// Achievement cell, Map marker ฯลฯ) — ใช้ร่วมกันได้ทุกแท็บเพื่อเลี่ยง
/// Destroy/Instantiate ทุกครั้งที่ rebuild list (กัน GC spike ตาม performance
/// requirement ของระบบเมนู)
///
/// วิธีใช้:
///   var pool = new UIListPool&lt;QuestListRowUI&gt;(prefab, container);
///   var item = pool.GetOrCreate(index);   // ดึง/สร้าง item ตำแหน่งที่ index
///   pool.ReleaseExtra(usedCount);          // ปิด item ที่เหลือเกินจากที่ใช้จริง
/// </summary>
public class UIListPool<T> where T : Component
{
    private readonly T _prefab;
    private readonly Transform _container;
    private readonly List<T> _instances = new();

    public UIListPool(T prefab, Transform container)
    {
        _prefab = prefab;
        _container = container;
    }

    public int ActiveCount { get; private set; }

    /// <summary>คืน instance ที่ index นี้ — สร้างใหม่ถ้ายังไม่มี, เปิดใช้งานเสมอ</summary>
    public T GetOrCreate(int index)
    {
        while (_instances.Count <= index)
        {
            T created = Object.Instantiate(_prefab, _container);
            _instances.Add(created);
        }

        T item = _instances[index];
        item.gameObject.SetActive(true);
        ActiveCount = Mathf.Max(ActiveCount, index + 1);
        return item;
    }

    /// <summary>ปิด (ไม่ Destroy) instance ทั้งหมดที่เกินจาก usedCount — recycle ไว้ใช้รอบหน้า</summary>
    public void ReleaseExtra(int usedCount)
    {
        for (int i = usedCount; i < _instances.Count; i++)
            _instances[i].gameObject.SetActive(false);
        ActiveCount = usedCount;
    }

    public void ReleaseAll() => ReleaseExtra(0);

    public IReadOnlyList<T> Instances => _instances;
}
