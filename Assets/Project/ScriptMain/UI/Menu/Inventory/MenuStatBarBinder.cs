using UnityEngine;

/// <summary>
/// Bridge เล็ก ๆ ที่ฟัง LocalPlayerStatSignal แล้วสั่งให้ StatBarUI (ตัวเดียวกับที่ใช้บน
/// HUD หลัก) bind เข้ากับ StatManager ของผู้เล่น local — แยกออกมาเป็นไฟล์เดียวเพื่อให้
/// StatBarUI ไม่ต้องรู้จัก Signal เลย (Single Responsibility)
/// </summary>
public class MenuStatBarBinder : MonoBehaviour
{
    [SerializeField] private LocalPlayerStatSignal statSignal;
    [SerializeField] private StatBarUI statBarUI;

    private void OnEnable()
    {
        statSignal.OnStatManagerReady += HandleReady;
        if (statSignal.CurrentStatManager != null) HandleReady(statSignal.CurrentStatManager);
    }

    private void OnDisable() => statSignal.OnStatManagerReady -= HandleReady;

    private void HandleReady(StatManager manager) => statBarUI.BindPlayer(manager);
}
