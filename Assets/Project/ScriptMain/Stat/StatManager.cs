using Unity.Netcode;
using UnityEngine;
public class StatManager : NetworkSaveableBehaviour
{
    [Header("Stat")]
    [SerializeField] public PlayerStatDataSO statsTemplate;
    [Tooltip("ใช้ให้ UI อื่น ๆ (เช่นแท็บ Inventory ในเมนูใหม่) bind เข้ากับ StatManager ตัวนี้ได้ — optional")]
    [SerializeField] private LocalPlayerStatSignal localStatSignal;
    public NetworkList<int> ActivePerkIds;

    public NetworkList<NetworkStat> AllStats;
    public NetworkList<NetworkKnowledgeStat> KnowledgeLevels;
    public override bool IsPlayerSaveable => true;
    private PlayerController _controller;

    public void Awake()
    {
        AllStats = new NetworkList<NetworkStat>();
        KnowledgeLevels = new NetworkList<NetworkKnowledgeStat>();
        ActivePerkIds = new NetworkList<int>();
        _controller = GetComponent<PlayerController>();
    }

    private void InitializeStat(StatType type, float max)
    {
        AllStats.Add(new NetworkStat
        {
            Type = type,
            CurrentValue = max,
            MaxValue = max
        });
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"[StatManager] OnNetworkSpawn — AllStats.Count: {AllStats.Count} IsServer:{IsServer}");
        if (IsServer)
        {
            if (AllStats.Count == 0)
            {
                InitializeStat(StatType.Health, statsTemplate.maxHealth);
                InitializeStat(StatType.Stamina, statsTemplate.maxStamina);
                InitializeStat(StatType.Energy, statsTemplate.maxEnergy);
            }

            foreach (RecipeCategory cat in System.Enum.GetValues(typeof(RecipeCategory)))
            {
                KnowledgeLevels.Add(new NetworkKnowledgeStat { Category = cat, Level = 1 });
            }
            SaveLoadManager.Instance?.Register(this);
        }
        if (IsOwner)
        {
            // Subscribe to scene events to rebind UI after every transition
            NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
            NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;

            // Bind immediately if UI already exists
            TryBindUI();
        }
    }

    private void TryBindUI()
    {
        if (PlayerUI.Instance != null)
        {
            PlayerUI.Instance.BindPlayer(this);
            Debug.Log($"[StatManager] UI bound — AllStats.Count: {AllStats.Count}");
        }

        localStatSignal?.UpdateStatManager(this);
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneEventType != SceneEventType.LoadEventCompleted) return;
        StartCoroutine(BindUINextFrame());
    }

    private System.Collections.IEnumerator BindUINextFrame()
    {
        yield return null;  // wait one frame for NGO to sync NetworkLists
        yield return null;  // wait one more frame to be safe
        TryBindUI();
    }

    public int GetLevelForCategory(RecipeCategory category)
    {
        foreach (var stat in KnowledgeLevels)
        {
            if (stat.Category == category) return stat.Level;
        }
        return 1;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsOwner)
            NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;

        if (IsServer)
            SaveLoadManager.Instance?.Unregister(this);
    }

    void Update()
    {
        if (!IsServer) return;


        if (_controller.IsRunning.Value)
        {
            Debug.Log("Detect running");
            float energyDrainAmount = statsTemplate.energyDrainRunning * Time.deltaTime;
            float staminaDrainAmount = statsTemplate.staminaDrainRunning * Time.deltaTime;
            ConsumeStat(StatType.Energy, energyDrainAmount);
            ConsumeStat(StatType.Stamina, staminaDrainAmount);
        }
        else
        {
            float energyDrainNormal = statsTemplate.energyDrainNormal * Time.deltaTime;
            ConsumeStat(StatType.Energy, energyDrainNormal);
            // Optional: Regen stamina when NOT running
            RegenStat(StatType.Stamina, statsTemplate.staminaRegenNormal * Time.deltaTime);
        }
    }

    public void RegenStat(StatType type, float amount)
    {
        if (!IsServer) return;

        for (int i = 0; i < AllStats.Count; i++)
        {
            if (AllStats[i].Type == type)
            {
                var stat = AllStats[i];
                if (stat.CurrentValue >= stat.MaxValue) return;
                stat.CurrentValue = Mathf.Clamp(stat.CurrentValue + amount, 0, stat.MaxValue);
                AllStats[i] = stat;
                return;
            }
        }
    }

    public void ConsumeStat(StatType type, float amount)
    {
        if (!IsServer) return;

        for (int i = 0; i < AllStats.Count; i++)
        {
            if (AllStats[i].Type == type)
            {
                // Debug.Log("Find stat");
                var stat = AllStats[i];
                if (stat.CurrentValue <= 0) return;
                stat.CurrentValue = Mathf.Clamp(stat.CurrentValue - amount, 0, stat.MaxValue);
                // Debug.Log($"New stat : {stat.CurrentValue}");
                AllStats[i] = stat;
                return;
            }
        }
    }

    [ServerRpc]
    private void UseItemServerRpc(int itemID)
    {
        var data = GameDataManager.Instance.itemDatabases.GetItemByID(itemID);
        if (data is not IUsable usable) return;

        // server validates AGAIN — client CanUse() is just UX, not security
        if (!usable.CanUse(GetComponent<StatManager>())) return;

        // usable.Use(GetComponent<StatManager>());
    }

    [ServerRpc]
    public void RequestConsumeEnergyServerRpc()
    {

    }

    public override void CaptureState(GameSaveData save, ulong clientId = 0)
    {
        var playerData = save.GetOrCreatePlayer(clientId);

        playerData.stats.Clear();
        foreach (var stat in AllStats)
        {
            playerData.stats.Add(new StatSaveData
            {
                type = stat.Type,
                currentValue = stat.CurrentValue,
                maxValue = stat.MaxValue
            });
        }

        // save level if you store it in StatManager
        // playerData.level = currentLevel.Value;
    }

    public override void RestoreState(GameSaveData save, ulong clientId = 0)
    {
        if (!IsServer) return;
        var playerData = save.FindPlayer(clientId);
        if (playerData == null) return;

        // rebuild AllStats NetworkList from save
        AllStats.Clear();
        foreach (var saved in playerData.stats)
        {
            AllStats.Add(new NetworkStat
            {
                Type = saved.type,
                CurrentValue = saved.currentValue,
                MaxValue = saved.maxValue
            });
        }
    }
}
