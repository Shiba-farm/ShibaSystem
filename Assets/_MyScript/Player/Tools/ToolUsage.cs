// ToolUsage.cs (Safe version - no input polling)
using UnityEngine;

public class ToolUsage : MonoBehaviour
{
    public PlayerEnergy playerEnergy;

    /// <summary>
    /// เรียกเมื่อต้องการ "พยายามหักพลังงาน"
    /// คืนค่า true = หักสำเร็จ, false = พลังงานไม่พอ (จะไม่หัก)
    /// </summary>
    public bool TrySpend(float cost)
    {
        if (!playerEnergy) return false;
        cost = Mathf.Max(0f, cost);
        if (cost <= 0f) return true;
        if (playerEnergy.CurrentEnergy < cost) return false;

        playerEnergy.UseEnergy(cost);
        return true;
    }
}
