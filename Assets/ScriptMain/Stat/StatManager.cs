using Unity.Netcode;
using UnityEngine;
public class StatManager : NetworkBehaviour
{
    [Header("Stat")]
    [SerializeField] public PlayerStatDataSO statsTemplate;
    public NetworkList<int> ActivePerkIds;

    public NetworkList<NetworkStat> AllStats;
    public NetworkList<NetworkKnowledgeStat> KnowledgeLevels;
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
        if (IsServer)
        {
            InitializeStat(StatType.Health, statsTemplate.maxHealth);
            InitializeStat(StatType.Stamina, statsTemplate.maxStamina);
            InitializeStat(StatType.Energy, statsTemplate.maxEnergy);

            foreach (RecipeCategory cat in System.Enum.GetValues(typeof(RecipeCategory)))
            {
                KnowledgeLevels.Add(new NetworkKnowledgeStat { Category = cat, Level = 1 });
            }
        }
        if (IsOwner)
        {
            if (PlayerUI.Instance != null)
            {
                PlayerUI.Instance.BindPlayer(this);
            }
            else
            {
                NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
            }
            // knowledgeSignal.UpdateKnowledgeSource(this);
        }
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
        {
            if (PlayerUI.Instance != null)
            {
                PlayerUI.Instance.BindPlayer(this);
            }
        }
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
}
