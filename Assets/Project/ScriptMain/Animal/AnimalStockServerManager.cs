using System;
using Unity.Netcode;
using UnityEngine;

public enum AnimalStockType
{
    None,
    Cattle,
    Sheep,
    Goat,
    Pig,
    Poultry,
    Equine,
    Other
}

/// <summary>
/// Scene-placed NetworkBehaviour singleton — server authority for the livestock shop.
/// Called by the UI script (e.g. AnimalStockPanelUI) with two entry points:
///
///   BuyLiveStockServerRpc(livestockName)
///     Buy → EntityDatabase (price lookup by name) → CurrencyManager (gold check/deduct)
///       → enough gold  → LivestockSpawnManager (spawn in front of player + give rope)
///       → not enough   → BuyLiveStockResultClientRpc(false, ...) so the UI can show the
///                        "you don't have enough gold" dialogue interrupt
///
///   FetchLiveStockDataServerRpc(type)
///     AnimalStockPanelUI → EntityDatabase (names by AnimalStockType)
///       → FetchLiveStockDataResultClientRpc(type, names[]) → UI displays the list
///
/// Both results come back only to the requesting client via RpcTarget.Single, and are
/// also exposed as C# events for the UI script to subscribe to directly.
/// </summary>
public class AnimalStockServerManager : NetworkBehaviour
{
    public static AnimalStockServerManager Instance { get; private set; }

    [Header("Data Source")]
    [SerializeField] private EntityDatabase entityDatabase;

    /// <summary>เรียกฝั่ง client เมื่อ BuyLiveStock เสร็จสิ้น (ไม่ว่าจะสำเร็จหรือไม่) — ใช้โดย UI script</summary>
    public event Action<bool, string> OnBuyLiveStockResult;

    /// <summary>เรียกฝั่ง client เมื่อ FetchLiveStockData ได้ผลลัพธ์กลับมา — ใช้โดย UI script</summary>
    public event Action<AnimalStockType, string[]> OnLiveStockDataFetched;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Buy ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// เรียกจาก client (UI script) ตอนผู้เล่นกดซื้อสัตว์ในร้าน — validate ทุกอย่างฝั่ง server:
    /// หาราคาจาก EntityDatabase → เช็คทองผ่าน CurrencyManager → พอ: หักทอง + spawn สัตว์ + ให้เชือก
    /// ไม่พอ: แจ้งกลับไป client เฉยๆ ไม่หัก ไม่ spawn
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void BuyLiveStockServerRpc(string livestockName, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (entityDatabase == null)
        {
            Debug.LogWarning("[AnimalStockServerManager] ยังไม่ได้ผูก Entity Database ใน Inspector");
            BuyLiveStockResultClientRpc(false, "Shop is not set up yet.", RpcTarget.Single(clientId, RpcTargetUse.Temp));
            return;
        }

        AnimalSO animal = entityDatabase.GetAnimalByName(livestockName);
        if (animal == null)
        {
            BuyLiveStockResultClientRpc(false, $"'{livestockName}' is not sold here.", RpcTarget.Single(clientId, RpcTargetUse.Temp));
            return;
        }

        int price = animal.price;

        if (CurrencyManager.Instance == null)
        {
            Debug.LogWarning("[AnimalStockServerManager] CurrencyManager.Instance เป็น null");
            BuyLiveStockResultClientRpc(false, "Shop is not set up yet.", RpcTarget.Single(clientId, RpcTargetUse.Temp));
            return;
        }

        long playerGold = CurrencyManager.Instance.CurrentGold;

        // Player have enough gold?
        if (playerGold < price)
        {
            BuyLiveStockResultClientRpc(false, "You don't have enough gold.", RpcTarget.Single(clientId, RpcTargetUse.Temp));
            return;
        }

        CurrencyManager.Instance.DeductCurrencyServerRpc(price);

        if (LivestockSpawnManager.Instance != null)
        {
            LivestockSpawnManager.Instance.SpawnLivestockForPlayer(animal, clientId);
        }
        else
        {
            Debug.LogWarning("[AnimalStockServerManager] LivestockSpawnManager.Instance เป็น null — หักทองไปแล้วแต่ spawn สัตว์ไม่ได้");
        }

        BuyLiveStockResultClientRpc(true, $"Bought {animal.animalName}!", RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void BuyLiveStockResultClientRpc(bool success, string message, RpcParams rpcParams = default)
    {
        OnBuyLiveStockResult?.Invoke(success, message);
    }

    // ── Fetch ────────────────────────────────────────────────────────────────

    /// <summary>
    /// เรียกจาก client (AnimalStockPanelUI) ตอนเปิดร้าน/เปลี่ยนแท็บ — ขอรายชื่อสัตว์ทั้งหมด
    /// ที่อยู่ใน AnimalStockType ที่ระบุ กลับไปแสดงในร้าน
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void FetchLiveStockDataServerRpc(AnimalStockType type, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        string[] names;
        if (entityDatabase == null)
        {
            Debug.LogWarning("[AnimalStockServerManager] ยังไม่ได้ผูก Entity Database ใน Inspector");
            names = Array.Empty<string>();
        }
        else
        {
            var animals = entityDatabase.GetAnimalsByType(type);
            names = new string[animals.Count];
            for (int i = 0; i < animals.Count; i++)
                names[i] = animals[i].animalName;
        }

        FetchLiveStockDataResultClientRpc(type, names, RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void FetchLiveStockDataResultClientRpc(AnimalStockType type, string[] names, RpcParams rpcParams = default)
    {
        OnLiveStockDataFetched?.Invoke(type, names);
    }
}
