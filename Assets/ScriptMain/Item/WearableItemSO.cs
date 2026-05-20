using UnityEngine;

[CreateAssetMenu(menuName = "Items/Wearable")]
public class WearableItemSO : ItemSO
{
    [Header("Wearable")]
    public EquipSlot slot;
    public float defenseBonus;
    public float speedBonus;
    public GameObject visualPrefab; 

    public EquipSlot Slot => slot;

    public void OnEquip(StatManager user)
    {
        // apply bonuses to StatManager
    }

    public void OnUnequip(StatManager user)
    {
        // remove bonuses from StatManager
    }
}
