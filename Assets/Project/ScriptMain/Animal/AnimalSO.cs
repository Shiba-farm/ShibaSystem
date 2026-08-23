using UnityEngine;

/// <summary>
/// Base ScriptableObject for any living creature in the world (animals, fish, etc.).
/// Stores identity, loot, and base stats that all animal types share.
///
/// Subclass this for creature-category-specific data:
///   FishSO  — adds fishing mini-game configuration
/// </summary>
[CreateAssetMenu(menuName = "Animals/Animal")]
public class AnimalSO : ScriptableObject
{
    [Header("Identity")]
    public int animalId;
    public string animalName;
    public Sprite icon;

    [Header("Loot (killed in world)")]
    [Tooltip("Items dropped when this animal is killed in the overworld or dungeon.")]
    public ItemSO[] killDropItems;

    [Header("Base Stats")]
    public int baseHealth = 10;
    public int baseAttack = 5;
    public float moveSpeed = 2f;
    public AnimalStockType type = AnimalStockType.LiveStock;

    [Header("Shop (Livestock Purchase)")]
    [Tooltip("ราคาซื้อ (Gold) ตอนซื้อจาก AnimalStockServerManager — ใช้เฉพาะ animal ที่ตั้งใจให้ขายเป็นสัตว์เลี้ยงในฟาร์ม")]
    public int price;

}
