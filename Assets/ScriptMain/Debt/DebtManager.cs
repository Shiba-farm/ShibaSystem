using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DebtManager : NetworkBehaviour, ISaveable
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

    private NetworkVariable<bool> isMonthSettled = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> tradeValuePaidThisMonth = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public int RemainingDueThisMonth => Mathf.Max(0, monthlyMinimumDue.Value - paidThisMonth.Value - tradeValuePaidThisMonth.Value);
    public int MonthlyMinimumDue => monthlyMinimumDue.Value;
    public int CurrentTradeValue => currentTradeValue.Value;
    public int PaidThisMonth => paidThisMonth.Value;
    public int TradePaidThisMonth => tradeValuePaidThisMonth.Value; // trade only
    public int TotalPaidThisMonth => paidThisMonth.Value + tradeValuePaidThisMonth.Value;
    public bool IsMonthSettled => isMonthSettled.Value;
    public int penalty { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public void SetPenalty(int amount)
    {
        penalty = amount;
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
        ApplyInterest();
        monthlyMinimumDue.Value = Mathf.RoundToInt(currentDebt.Value *
            Mathf.Max(MinimumPaymentPercent, GetMonthlyPaymentRate()));
        paidThisMonth.Value = 0;
        tradeValuePaidThisMonth.Value = 0;
        isMonthSettled.Value = false;
    }

    public void SetMonthSettled(bool value)
    {
        if (!IsServer) return;
        isMonthSettled.Value = value;
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
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void MakeTradePaymentServerRpc()
    {
        int amount = currentTradeValue.Value;
        if (amount <= 0) return;

        tradeValuePaidThisMonth.Value += amount; // ← separate tracker
        currentTradeValue.Value = 0;
        currentDebt.Value = Mathf.Max(0, currentDebt.Value - amount);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddDebtPunishmentServerRpc(int amount)
    {
        if (!IsServer) return;
        currentDebt.Value += amount;
        Debug.Log($"[DebtManager] Punishment debt added: +{amount}, total: {currentDebt.Value}");
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetMonthSettledServerRpc()
    {
        isMonthSettled.Value = true;
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

    public void ApplyPunishment(PunishmentResult result)
    {
        int remaining = RemainingDueThisMonth;
        int penalty = Mathf.RoundToInt(remaining * 1.5f);

        Debug.Log($"[Penalty] Remaining={remaining}, Penalty={penalty}, {tradeValuePaidThisMonth.Value}, {paidThisMonth.Value}"); // add this
        result.AddedDebt = penalty;
        SetPenalty(penalty);
        AddDebtPunishmentServerRpc(penalty);
    }

    private void CheckDefaultCondition()
    {
        if (currentDebt.Value <= 0)
        {
            Debug.Log("[DebtManager] Debt fully paid! Player wins.");
            return;
        }
    }

    public float GetMonthlyPaymentRate() => DifficultyPaymentRate[GameDataManager.Instance.CurrentDifficulty];

    public void CaptureState(GameSaveData save, ulong clientId = 0)
    {
        save.world.currentDebt = CurrentDebt;
        save.world.monthlyMinimumDue = MonthlyMinimumDue;
        save.world.paidThisMonth = PaidThisMonth;
        save.world.tradeValuePaidThisMonth = TradePaidThisMonth;
    }

    public void RestoreState(GameSaveData save, ulong clientId = 0)
    {
        if (!IsServer) return;
        currentDebt.Value = save.world.currentDebt;
        monthlyMinimumDue.Value = save.world.monthlyMinimumDue;
        paidThisMonth.Value = save.world.paidThisMonth;
        tradeValuePaidThisMonth.Value = save.world.tradeValuePaidThisMonth;
    }
}

public class PunishmentResult
{
    public List<(ItemSO item, int amount)> LostItems { get; } = new();
    public int AddedDebt { get; set; } = 0;
    public int GoldPaid { get; set; } = 0;      // ← snapshot at trade time
    public int TradePaid { get; set; } = 0;     // ← snapshot at trade time
    public int FinalDebt { get; set; } = 0;
    public bool WasRefused { get; set; } = false;

    public bool HasLostItems => LostItems.Count > 0;
    public bool HasAddedDebt => AddedDebt > 0;

}