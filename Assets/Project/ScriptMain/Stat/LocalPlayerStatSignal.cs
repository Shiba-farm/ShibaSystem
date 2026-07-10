using System;
using UnityEngine;

/// <summary>
/// Signal เดียวกันแนวคิด CurrencySignal/InventoryDataSignal — แต่สำหรับ StatManager
/// ของผู้เล่น local เพื่อให้ UI หลายที่ (HUD หลัก + แท็บ Inventory ในเมนูใหม่)
/// bind เข้ากับ StatManager ตัวเดียวกันได้โดยไม่ต้องพึ่ง PlayerUI.Instance ที่เป็น
/// singleton ตัวเดียว — StatManager เป็นคน push เข้ามาเองตอน IsOwner เท่านั้น
/// (ดู StatManager.TryBindUI) จึงปลอดภัยต่อ multiplayer เหมือน signal อื่น ๆ
/// </summary>
[CreateAssetMenu(menuName = "Signals/LocalPlayerStatSignal")]
public class LocalPlayerStatSignal : ScriptableObject
{
    public event Action<StatManager> OnStatManagerReady;
    public StatManager CurrentStatManager { get; private set; }

    public void UpdateStatManager(StatManager manager)
    {
        CurrentStatManager = manager;
        OnStatManagerReady?.Invoke(manager);
    }
}
