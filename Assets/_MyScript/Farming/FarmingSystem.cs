using UnityEngine;
using UnityEngine.Audio;
using Sirenix.OdinInspector;

public class FarmingSystem : SerializedMonoBehaviour
{
    [Title("Farming Settings")]
    [Required] public Camera cam;
    public LayerMask soilMask;
    public LayerMask treeMask;
    [Range(1f, 8f)] public float interactRange = 4f;
    public bool enableInternalInput = false;

    [Title("Effects & Audio")]
    public float effectHeightOffset = 0.2f;
    public AudioClip harvestSFX;
    public AudioMixerGroup sfxMixerGroup;

    [FoldoutGroup("Runtime"), ReadOnly]
    private PlayerEnergy energy;
    private Transform playerTransform;

    private void Awake()
    {
        if (!cam) cam = Camera.main;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p) playerTransform = p.transform;
        energy = FindObjectOfType<PlayerEnergy>();
    }

    private void Update()
    {
        if (!enableInternalInput) return;
        if (Input.GetMouseButtonDown(0)) HandlePrimaryAction();
        if (Input.GetMouseButtonDown(1)) TryHarvest();
    }

    // API
    public bool TryGetTargetSoil(out SoilTile tile) => TryHitSoil(out tile);
    public bool TryGetTargetTree(out ChoppableCut_Tree tree) => TryHitTree(out tree);

    public void ChopTree(ItemSO axeItem, ChoppableCut_Tree tree)
    {
        if (!axeItem || !tree) return;
        // ���Ѵ����Ẻ FlatPos ���ͻ�ͧ�ѹ�ѭ�ҵ���Ф��׹�����٧/��ӡ��ҵ����
        if (playerTransform && Vector3.Distance(FlatPos(playerTransform.position), FlatPos(tree.transform.position)) > interactRange) return;

        // // float cost = Mathf.Max(0, axeItem.energyCost);
        // if (energy && energy.CurrentEnergy < cost) return;

        // tree.GetHit(1f);
        // PlayActionEffects(axeItem, tree.transform.position);

        // if (energy) energy.UseEnergy(cost);
    }

    public void ApplyItemOnTile(ItemSO item, SoilTile tile)
    {
        if (!item || !tile) return;
        // ���Ѵ����Ẻ FlatPos 
        if (playerTransform && Vector3.Distance(FlatPos(playerTransform.position), FlatPos(tile.transform.position)) > interactRange) return;

        switch (item.category)
        {
            case ItemCategory.Tools: UseTool(item, tile); break;
            case ItemCategory.Seed: PlantSeed(item, tile); break;
        }
    }

    public void TryHarvestExternal(SoilTile specificTile = null)
    {
        SoilTile tileToHarvest = specificTile;
        if (tileToHarvest == null) if (!TryHitSoil(out tileToHarvest)) return;

        // ���Ѵ����Ẻ FlatPos 
        if (playerTransform && Vector3.Distance(FlatPos(playerTransform.position), FlatPos(tileToHarvest.transform.position)) > interactRange) return;

        bool AddToInventory(ItemSO item, int amount)
        {
            bool success = false;
            if (InventoryMainUI.Instance && InventoryMainUI.Instance.AddItemToInventory(item, amount)) success = true;
            else if (HotbarUI.Instance && HotbarUI.Instance.AddItemToFirstEmptySlot(item, amount)) success = true;
            if (success && harvestSFX != null) PlaySoundWithMixer(harvestSFX, tileToHarvest.transform.position, 0f, 1f);
            return success;
        }
        tileToHarvest.HarvestToInventory(AddToInventory);
    }

    // INTERNAL LOGIC
    void HandlePrimaryAction() { if (!HotbarUI.Instance) return; var item = HotbarUI.Instance.GetSelectedItem(); if (!item) return; if (!TryHitSoil(out var tile)) return; ApplyItemOnTile(item, tile); }

    void UseTool(ItemSO tool, SoilTile tile)
    {
        // float cost = Mathf.Max(0, tool.energyCost);
        // if (energy && energy.CurrentEnergy < cost) return;
        // bool success = false;
        // if (tool.toolAction == ToolAction.Hoe) { tile.Till(); success = true; }
        // if (tool.toolAction == ToolAction.Water && tile.isTilled) { tile.Water(); success = true; }
        // if (success) { PlayActionEffects(tool, tile.transform.position); if (energy) energy.UseEnergy(cost); }
    }

    void PlantSeed(ItemSO seedItem, SoilTile tile)
    {
        // if (!seedItem.seedCrop) return;
        // if (!HotbarUI.Instance) return;
        // var slot = HotbarUI.Instance.GetSelectedSlot();
        // if (slot == null || slot.amount <= 0) return;
        // if (!tile.CanPlant(seedItem.seedCrop)) return;
        // float cost = Mathf.Max(0, seedItem.energyCost);
        // if (energy && energy.CurrentEnergy < cost) return;
        // tile.Plant(seedItem.seedCrop); PlayActionEffects(seedItem, tile.transform.position);
        // if (energy) energy.UseEnergy(cost); slot.amount -= 1; if (slot.amount <= 0) slot.Clear(); else slot.UpdateUI();
    }

    void TryHarvest() => TryHarvestExternal(null);

    // ===========================================
    // HELPERS (Snap to Ground + Play + Destroy)
    // ===========================================
    void PlayActionEffects(ItemSO item, Vector3 targetPos)
    {
        // 1. ��駨ش�ԧ�������٧ 500 ���� (���ԡѴ X, Z ���)
        Vector3 rayOrigin = new Vector3(targetPos.x, 500f, targetPos.z);
        Vector3 spawnPos = targetPos;
        RaycastHit hit;

        // 2. �ԧ Raycast ŧ�� (Vector3.down) ���� 1000 ����
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 1000f, ~0, QueryTriggerInteraction.Ignore))
        {
            spawnPos = hit.point + Vector3.up * effectHeightOffset;
        }

        // // --- ��ǹ���ҧ VFX ---
        // if (item.actionVFX)
        // {
        //     GameObject vfxObj = Instantiate(item.actionVFX, spawnPos, Quaternion.identity);
        //     ParticleSystem ps = vfxObj.GetComponent<ParticleSystem>();
        //     if (ps != null)
        //     {
        //         ps.Play();
        //         Destroy(vfxObj, ps.main.duration + ps.main.startLifetime.constantMax + 0.2f);
        //     }
        //     else
        //     {
        //         Destroy(vfxObj, 2f);
        //     }
        // }

        // if (item.actionSFX)
        // {
        //     float duration = item.sfxDuration;
        //     float pitch = Random.Range(1f, item.pitchRandomMultiplier);
        //     PlaySoundWithMixer(item.actionSFX, spawnPos, duration, pitch);
        // }
    }

    void PlaySoundWithMixer(AudioClip clip, Vector3 position, float durationLimit = 0f, float pitch = 1f)
    {
        if (!clip) return; GameObject audioObj = new GameObject("TempAudio_" + clip.name); audioObj.transform.position = position;
        AudioSource source = audioObj.AddComponent<AudioSource>(); source.clip = clip; source.spatialBlend = 0f; source.volume = 1f; source.pitch = pitch;
        if (sfxMixerGroup != null) source.outputAudioMixerGroup = sfxMixerGroup;
        source.Play(); float lifeTime = clip.length; if (durationLimit > 0f && durationLimit < lifeTime) lifeTime = durationLimit; Destroy(audioObj, lifeTime + 0.1f);
    }

    // ===========================================
    // [�ѻ�ô] �Ѵ᡹ Y ��� ���������������������� 1000f
    // ===========================================
    private Vector3 FlatPos(Vector3 pos)
    {
        return new Vector3(pos.x, 0, pos.z);
    }

    bool TryHitSoil(out SoilTile tile)
    {
        tile = null;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // �ѻ�ô��������������� 1000f
        if (Physics.Raycast(ray, out var hit, 1000f, soilMask))
        {
            // �� FlatPos �Ѵ����
            if (playerTransform == null || Vector3.Distance(FlatPos(playerTransform.position), FlatPos(hit.point)) <= interactRange)
            {
                // �� GetComponentInParent ���ͤ�ԡⴹ�����١
                tile = hit.collider.GetComponentInParent<SoilTile>();
            }
        }
        return tile != null;
    }

    bool TryHitTree(out ChoppableCut_Tree tree)
    {
        tree = null;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // �ѻ�ô��������������� 1000f
        if (Physics.Raycast(ray, out var hit, 1000f, treeMask))
        {
            // �� FlatPos �Ѵ����
            if (playerTransform == null || Vector3.Distance(FlatPos(playerTransform.position), FlatPos(hit.point)) <= interactRange)
            {
                // �� GetComponentInParent ���ͤ�ԡⴹ�����١
                tree = hit.collider.GetComponentInParent<ChoppableCut_Tree>();
            }
        }
        return tree != null;
    }
}