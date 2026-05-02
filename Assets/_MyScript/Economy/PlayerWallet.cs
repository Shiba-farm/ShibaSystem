using UnityEngine;
using System;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }

    [SerializeField] private int money = 0;

    public int Money => money;
    public int Balance => money;                      // <— เพิ่ม alias ให้สคริปต์อื่นเรียกได้

    public event Action<int> OnMoneyChanged;

    private void Awake()
    {
        Instance = this;
    }

    public void SetMoney(int amount)                 // <— เพิ่ม สำหรับ Load เกม
    {
        money = Mathf.Max(0, amount);
        OnMoneyChanged?.Invoke(money);
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;
        money += amount;
        OnMoneyChanged?.Invoke(money);
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0) return false;
        if (money < amount) return false;
        money -= amount;
        OnMoneyChanged?.Invoke(money);
        return true;
    }
}
