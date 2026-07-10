using System;
using UnityEngine;

/// <summary>
/// สะพานเชื่อม EquipmentData (ของผู้เล่น local เท่านั้น) เข้ากับ UI — รูปแบบเดียวกับ
/// InventoryDataSignal ทุกประการ ทำให้ UI ไม่มีทาง bind เข้ากับ equipment ของ
/// ผู้เล่นคนอื่นได้เลย เพราะ EquipmentData จะเรียก UpdateEquipmentData(this) ก็ต่อเมื่อ
/// IsOwner เท่านั้น (ดู EquipmentData.OnNetworkSpawn)
/// </summary>
[CreateAssetMenu(menuName = "Signals/EquipmentDataSignal")]
public class EquipmentDataSignal : ScriptableObject
{
    public event Action<EquipmentData> OnDataUpdate;
    public EquipmentData CurrentData { get; private set; }

    public void UpdateEquipmentData(EquipmentData data)
    {
        CurrentData = data;
        OnDataUpdate?.Invoke(data);
    }
}
