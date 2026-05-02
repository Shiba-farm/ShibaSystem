using UnityEngine;

public class SoilTile : MonoBehaviour
{
    [Header("State")]
    public bool isTilled;
    public bool isWatered;

    [Header("Crop Runtime")]
    public CropSO crop;
    public int stageIndex;
    public float stageTimer;

    [Header("Refs")]
    [Tooltip("�ش�Դ����� (��������ҧ������˹� GameObject ���)")]
    public Transform cropParent;

    [Header("Ground Visuals")]
    [Tooltip("�ش�Դ prefab �Թ (��������ҧ������˹� GameObject ���)")]
    public Transform groundParent;

    [Tooltip("Prefab �Թ��� (��ѧ�ǹ�Թ)")]
    public GameObject groundDryPrefab;

    [Tooltip("Prefab �Թ��¡ (��ѧô���)")]
    public GameObject groundWetPrefab;

    private GameObject currentCropObj;
    private GameObject currentGroundObj;

    // ================== STATE CHANGE ==================

    /// <summary>
    /// �ǹ�Թ�����á -> ���Թ�����繴Թ���
    /// </summary>
    public void Till()
    {
        if (isTilled) return;           // �������á
        isTilled = true;
        isWatered = false;
        UpdateGroundVisual();
    }

    /// <summary>
    /// ô��� -> ��ͧ䶡�͹�֧��ô��, ��������¹�Թ�繴Թ��¡
    /// </summary>
    public void Water()
    {
        if (!isTilled) return;          // �ѧ���ǹ�Թ ����ô
        isWatered = true;
        UpdateGroundVisual();
    }

    public bool CanPlant(CropSO c)
    {
        // �ѧ�����͹����: ��ͧ�����, ����վת����, ��� Crop ����� null
        // �����ҡ��� "��ͧô��ӡ�͹��١" ������� && isWatered ������
        return isTilled && crop == null && c != null;
    }

    public void Plant(CropSO c)
    {
        if (!CanPlant(c)) return;

        crop = c;
        stageIndex = 0;
        stageTimer = 0f;
        // ������� isWatered ����������͡ flow ����
        // - � -> ��١ -> ô��� (Ẻ Stardew)
        // - � -> ô��� -> ��١ (Ẻ���س��ҡ��)
        SpawnCropStage();
    }

    // ================== HARVEST ==================

    public bool CanHarvest()
    {
        if (crop == null) return false;
        return stageIndex >= crop.growthPrefabs.Length - 1 && crop.harvestItem != null;
    }

    public int HarvestToInventory(System.Func<ItemSO, int, bool> addFunc)
    {
        if (!CanHarvest()) return 0;

        int amount = Random.Range(crop.yieldRange.x, crop.yieldRange.y + 1);
        amount = Mathf.Max(0, amount);

        bool added = addFunc?.Invoke(crop.harvestItem, amount) ?? false;

        if (added && crop.destroyOnHarvest)
        {
            // �����ǴԹ��Ѻ�� "�ѧ���ǹ"
            ClearCrop();
        }
        else if (added)
        {
            // ���������� �����͹��Ѻ� stage ��͹�ش����
            stageIndex = Mathf.Max(0, crop.growthPrefabs.Length - 2);
            stageTimer = 0f;
            isWatered = false;
            SpawnCropStage();
        }

        return added ? amount : 0;
    }

    public void ClearCrop()
    {
        crop = null;
        stageIndex = 0;
        stageTimer = 0f;
        isWatered = false;
        isTilled = false;

        if (currentCropObj) Destroy(currentCropObj);
        currentCropObj = null;

        UpdateGroundVisual(); // ������Թ������仴���
    }

    // ================== UPDATE GROWTH ==================

    void Update()
    {
        if (crop == null) return;
        if (!isTilled) return;

        // ���੾�е͹���ô������� (��� Crop �к���ҵ�ͧ���)
        bool canGrow = !crop.requiresWaterEachStage || isWatered;
        if (!canGrow) return;

        // [FIX] ใช้ game-time แทน real-time เพื่อให้พืชโต sync กับเวลาในเกม
        if (TimeOfDaySystem.Instance != null)
            stageTimer += TimeOfDaySystem.Instance.GameHoursDelta;
        else
            stageTimer += Time.deltaTime;
        float target = crop.stageDurations[Mathf.Clamp(stageIndex, 0, crop.stageDurations.Length - 1)];

        if (stageTimer >= target)
        {
            stageTimer = 0f;
            isWatered = false;

            if (stageIndex < crop.growthPrefabs.Length - 1)
            {
                stageIndex++;
                SpawnCropStage();
            }
        }
    }

    void SpawnCropStage()
    {
        if (currentCropObj) Destroy(currentCropObj);

        if (crop != null && stageIndex < crop.growthPrefabs.Length)
        {
            var prefab = crop.growthPrefabs[stageIndex];
            if (!prefab) return;

            Transform parent = cropParent ? cropParent : transform;
            currentCropObj = Instantiate(
                prefab,
                parent.position,
                Quaternion.identity,
                parent
            );
        }
    }

    // ================== GROUND VISUALS ==================

    void UpdateGroundVisual()
    {
        if (currentGroundObj) Destroy(currentGroundObj);

        if (!isTilled)
        {
            // �ѧ���ǹ�Թ -> ����� prefab �Թ
            return;
        }

        GameObject prefab = isWatered ? groundWetPrefab : groundDryPrefab;
        if (!prefab) return;

        Transform parent = groundParent ? groundParent : transform;
        currentGroundObj = Instantiate(prefab, parent.position, Quaternion.identity, parent);
    }

    // ================== SAVE / LOAD SUPPORT ==================

    public SoilTileData GetSaveData()
    {
        SoilTileData d = new SoilTileData();

        Vector3 p = transform.position;
        d.posX = p.x;
        d.posY = p.y;
        d.posZ = p.z;

        d.isTilled = isTilled;
        d.isWatered = isWatered;

        d.cropName = crop ? crop.cropName : "";
        d.stageIndex = stageIndex;
        d.stageTimer = stageTimer;

        return d;
    }

    public void ApplySaveData(SoilTileData d, CropSO[] allCrops)
    {
        isTilled = d.isTilled;
        isWatered = d.isWatered;

        if (string.IsNullOrEmpty(d.cropName))
        {
            // [FIX] ไม่เรียก ClearCrop() เพราะมันจะ reset isTilled = false
            // ให้ clear เฉพาะ crop object แต่คงสถานะดินจาก save
            crop = null;
            stageIndex = 0;
            stageTimer = 0f;
            if (currentCropObj) { Destroy(currentCropObj); currentCropObj = null; }
            isTilled  = d.isTilled;
            isWatered = d.isWatered;
            UpdateGroundVisual();
            return;
        }

        CropSO target = null;
        foreach (var c in allCrops)
        {
            if (c != null && c.cropName == d.cropName)
            {
                target = c;
                break;
            }
        }

        if (target == null)
        {
            Debug.LogWarning($"Crop '{d.cropName}' not found!");
            ClearCrop();
            return;
        }

        crop = target;
        stageIndex = Mathf.Clamp(d.stageIndex, 0, crop.growthPrefabs.Length - 1);
        stageTimer = d.stageTimer;

        SpawnCropStage();
        UpdateGroundVisual(); // ���Թ��Ѻ��������ʶҹз��૿���
    }
}
