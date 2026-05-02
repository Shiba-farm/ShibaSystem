using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    [Tooltip("�ҡ ItemSO �����������")]
    public ItemSO[] items;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[ItemDatabase] พบ Instance ซ้ำบน '{gameObject.name}' — ลบ Component นี้ออก");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public ItemSO GetItemByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        foreach (var it in items)
        {
            if (it != null && it.itemName == name)
                return it;
        }

        Debug.LogWarning("[ItemDatabase] Item not found: " + name);
        return null;
    }
}
