using UnityEngine;

/// <summary>
/// ScriptableObject กำหนดชนิดปลาและ % โอกาสตกได้ในแต่ละโซน
/// สร้างผ่าน Assets > Create > ShibaFarm > Fishing > Fishing Zone SO
/// </summary>
[CreateAssetMenu(fileName = "FishingZone_New", menuName = "ShibaFarm/Fishing/Fishing Zone SO")]
public class FishingZoneSO : ScriptableObject
{
    [System.Serializable]
    public class FishEntry
    {
        public ItemSO fish;
        [Range(0f, 100f)] public float chance = 50f;
        [Min(1)] public int minAmount = 1;
        [Min(1)] public int maxAmount = 1;
    }

    [Header("Zone Info")]
    public string zoneName = "แม่น้ำ";

    [Header("Fish Table")]
    [Tooltip("รายชื่อปลาและ % โอกาส — ระบบ normalize ให้อัตโนมัติ")]
    public FishEntry[] fishTable;

    /// <summary>สุ่มปลาจาก fishTable โดย weighted random</summary>
    public FishEntry RollFish()
    {
        if (fishTable == null || fishTable.Length == 0) return null;

        float total = 0f;
        foreach (var e in fishTable)
            if (e.fish != null) total += e.chance;

        if (total <= 0f) return null;

        float roll = Random.Range(0f, total);
        float cumulative = 0f;
        foreach (var e in fishTable)
        {
            if (e.fish == null) continue;
            cumulative += e.chance;
            if (roll <= cumulative) return e;
        }

        return fishTable[fishTable.Length - 1];
    }
}
