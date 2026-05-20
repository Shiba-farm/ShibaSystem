using UnityEngine;

public interface IEquippable
{
    EquipSlot Slot { get; }          // Head, Chest, Legs, Feet
    void OnEquip(StatManager user);  // apply stat buffs
    void OnUnequip(StatManager user);
}

public enum EquipSlot { Head, Chest, Legs, Feet }