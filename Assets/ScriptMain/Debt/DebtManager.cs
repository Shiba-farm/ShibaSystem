using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DebtManager : NetworkBehaviour
{
    public static DebtManager Instance { get; private set; }
    public event Action OnDebtChanged;
    public event Action OnTradeValueChanged;

    private static readonly Dictionary<GameDifficulty, float> DifficultyPaymentRate = new()
    {
        { GameDifficulty.Easy,   0.10f },
        { GameDifficulty.Normal, 0.15f },
        { GameDifficulty.Hard,   0.20f }
    };
    private static readonly Dictionary<GameDifficulty, float> DifficultyInterestRate = new()
    {
        { GameDifficulty.Easy,   0.03f },  // 3%
        { GameDifficulty.Normal, 0.05f },  // 5%
        { GameDifficulty.Hard,   0.08f },  // 8%
    };
    private float MonthlyInterestRate =>
        DifficultyInterestRate[GameDataManager.Instance.CurrentDifficulty];

    private NetworkVariable<int> currentDebt = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField] private int startingDebt = 10000;
    [SerializeField] private float monthlyInterestRate = 0.05f;   // 5% per month
    [SerializeField] private int monthlyMinimumPayment = 500;

    public int CurrentDebt => currentDebt.Value;
    private const float MinimumPaymentPercent = 0.10f;
    public int MinimumPaymentDue => Mathf.Max(
        Mathf.RoundToInt(currentDebt.Value * MinimumPaymentPercent),
        Mathf.RoundToInt(currentDebt.Value * GetMonthlyPaymentRate())
    );

    private NetworkVariable<int> monthlyMinimumDue = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> paidThisMonth = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> currentTradeValue = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public int RemainingDueThisMonth => Mathf.Max(0, monthlyMinimumDue.Value - paidThisMonth.Value);
    public int MonthlyMinimumDue => monthlyMinimumDue.Value;
    public int CurrentTradeValue => currentTradeValue.Value;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentDebt.Value = startingDebt;

        currentDebt.OnValueChanged += (prev, next) => OnDebtChanged?.Invoke();
        currentTradeValue.OnValueChanged += (prev, next) => OnTradeValueChanged?.Invoke();
        monthlyMinimumDue.Value = MinimumPaymentDue;
    }

    // Called at end of each month by WorldTimeManager
    public void OnMonthEnd()
    {
        if (!IsServer) return;

        // 1. Interest first
        ApplyInterest();

        monthlyMinimumDue.Value = Mathf.RoundToInt(currentDebt.Value *
        Mathf.Max(MinimumPaymentPercent, GetMonthlyPaymentRate()));
        paidThisMonth.Value = 0;
    }

    private void ApplyInterest()
    {
        int interest = Mathf.RoundToInt(currentDebt.Value * monthlyInterestRate);
        currentDebt.Value += interest;
        Debug.Log($"[DebtManager] Interest applied: +{interest}, total debt: {currentDebt.Value}");
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void MakePaymentServerRpc(int amount)
    {
        if (amount <= 0) return;

        long playerGold = CurrencyManager.Instance.CurrentGold;
        paidThisMonth.Value += amount;

        if (amount > playerGold)
        {
            return;
        }

        ExecutePayment(amount);
    }

    public void AddTradeValue(int amount)
    {
        if (amount <= 0) return;

        currentTradeValue.Value += amount;
    }
    public void DeductTradeValue(int amount)
    {
        if (amount <= 0) return;

        currentTradeValue.Value -= amount;
    }

    private void ExecutePayment(int amount)
    {
        CurrencyManager.Instance.DeductCurrencyServerRpc(amount);
        currentDebt.Value = Mathf.Max(0, currentDebt.Value - amount);

        Debug.Log($"[DebtManager] Payment of {amount} made. Remaining debt: {currentDebt.Value}");

        if (currentDebt.Value <= 0)
        {
            Debug.Log("[DebtManager] Debt fully cleared — trigger win condition!");
            // GameManager.Instance.TriggerWinCondition();
        }
    }

    // private void ApplyPunishment(int playerGold, int minimumDue)
    // {
    //     Debug.Log($"[DebtManager] PUNISHMENT — player has {playerGold} gold but owes {minimumDue}. Can't pay minimum!");
    //     // TODO: replace with real punishment logic
    //     // e.g. increase debt penalty, trigger negative event, etc.
    // }

    private void CheckDefaultCondition()
    {
        if (currentDebt.Value <= 0)
        {
            Debug.Log("[DebtManager] Debt fully paid! Player wins.");
            return;
        }
    }

    public float GetMonthlyPaymentRate() => DifficultyPaymentRate[GameDataManager.Instance.CurrentDifficulty];
}
