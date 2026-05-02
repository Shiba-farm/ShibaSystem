using UnityEngine;

public class ItemMagnet : MonoBehaviour
{
    [Header("Settings")]
    public float delayBeforeMagnet = 0.5f; // ���꺹֧���´ٴ (����ѹ��Ш�¡�͹)
    public float magnetSpeed = 10f;
    public float pickupRadius = 1f; // ���з��ж���������
    public ItemSO itemToGive;       // ����������ҡ�����
    public int amount = 1;

    private Transform player;
    private float spawnTime;
    private bool isSucking = false;

    void Start()
    {
        spawnTime = Time.time;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
    }

    void Update()
    {
        if (!player) return;

        // �����Ҵ������͹����������ٴ
        if (Time.time < spawnTime + delayBeforeMagnet) return;

        // �ӹǳ������ҧ
        float dist = Vector3.Distance(transform.position, player.position);

        // �����������ҡ -> �红ͧ��ҵ��
        if (dist <= pickupRadius)
        {
            Collect();
            return;
        }

        // ����͹�������Ҽ�����
        transform.position = Vector3.MoveTowards(transform.position, player.position + Vector3.up, magnetSpeed * Time.deltaTime);
    }

    void Collect()
    {
        int remaining = amount;

        // 1) ลอง Hotbar ก่อน (อยู่ใกล้มือ สะดวกกว่า)
        if (remaining > 0 && HotbarUI.Instance != null)
        {
            bool added = HotbarUI.Instance.AddItemToFirstEmptySlot(itemToGive, remaining);
            if (added) remaining = 0;
        }

        // 2) ที่เหลือใส่ Inventory
        if (remaining > 0 && InventoryMainUI.Instance != null)
        {
            InventoryMainUI.Instance.AddItemToInventory(itemToGive, remaining);
        }

        Destroy(gameObject);
    }
}