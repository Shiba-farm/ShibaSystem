using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// แสดงโมเดล 3D ของผู้เล่น local แบบ real-time มุมซ้ายของหน้า Inventory
/// วิธีทำงาน: ใช้ Camera แยก (culling mask = layer พิเศษ เช่น "UIPreview") render
/// ลง RenderTexture แล้วเอาไปแสดงบน RawImage — ไม่ยุ่งกับกล้องหลักของเกมเลย
///
/// ทำงานร่วมกับ EquipmentDataSignal: ทุกครั้งที่ EquippedItems เปลี่ยน จะสั่ง
/// PlayerPreviewRig (ผ่าน IEquipmentVisualApplier) ให้สวม/ถอดของให้ตรงกับของจริง
/// </summary>
public class PlayerPreviewUI : MonoBehaviour
{
    [Header("Render Target")]
    [SerializeField] private Camera previewCamera;
    [SerializeField] private RawImage targetImage;

    [Header("Rig")]
    [SerializeField] private GameObject fallbackRigPrefab; // ใช้ตอนยังไม่เจอโมเดลผู้เล่นจริง (เช่น demo/sample)
    [SerializeField] private Transform rigSpawnPoint;

    [Header("Data")]
    [SerializeField] private EquipmentDataSignal equipmentSignal;

    private GameObject _rigInstance;
    private IEquipmentVisualApplier _visualApplier;
    private EquipmentData _activeData;
    private bool _needsRefresh;

    private void OnEnable()
    {
        if (previewCamera != null) previewCamera.enabled = true;
        EnsureRigSpawned();

        equipmentSignal.OnDataUpdate += HandleConnected;
        if (equipmentSignal.CurrentData != null) HandleConnected(equipmentSignal.CurrentData);
    }

    private void OnDisable()
    {
        if (previewCamera != null) previewCamera.enabled = false; // ไม่ render ตอนแท็บถูกปิด — ประหยัด GPU

        equipmentSignal.OnDataUpdate -= HandleConnected;
        if (_activeData != null) _activeData.EquippedItems.OnListChanged -= HandleListChanged;
        // null ออกเพื่อให้ OnEnable ครั้งถัดไป re-subscribe OnListChanged ใหม่
        _activeData = null;
    }

    private void EnsureRigSpawned()
    {
        if (_rigInstance != null) return;

        // ใช้ fallbackRigPrefab โดยตรง — ข้าม LocalPlayerVisualRegistry
        // เหตุผล: LocalPlayerVisualMarker อาจติดอยู่กับ sub-object (เช่น Mouth.001)
        // ซึ่งไม่ใช่ตัวละครทั้งตัว ทำให้ clone ได้แค่ mesh ปากแทน
        // fallbackRigPrefab (PlayerModel_UIPreview) มี PlayerPreviewRig และอยู่ layer UIPreview แล้ว
        GameObject prefabOrInstance = fallbackRigPrefab;
        if (prefabOrInstance == null)
        {
            Debug.LogWarning("[PlayerPreviewUI] ไม่มี fallbackRigPrefab — ลาก PlayerModel_UIPreview ใส่ช่อง Fallback Rig Prefab");
            return;
        }

        _rigInstance = Instantiate(prefabOrInstance, rigSpawnPoint);
        _rigInstance.transform.localPosition = Vector3.zero;
        // หมุน 180° บน Y เพื่อให้ตัวละครหันหน้าเข้าหากล้อง
        // (โมเดล Shiba มี forward = +Z เดียวกับกล้อง ต้องกลับหน้า)
        _rigInstance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        // ── ตั้ง Layer ทุก child ให้เป็น UIPreview เพื่อให้ PreviewCamera (Culling Mask=UIPreview) มองเห็น ──
        int uiPreviewLayer = LayerMask.NameToLayer("UIPreview");
        if (uiPreviewLayer >= 0)
            SetLayerRecursive(_rigInstance, uiPreviewLayer);
        else
            Debug.LogWarning("[PlayerPreviewUI] Layer 'UIPreview' ไม่พบ — ไปที่ Edit > Project Settings > Tags and Layers แล้วเพิ่ม");

        // ใช้ GetComponentInChildren เผื่อ PlayerPreviewRig อยู่ที่ child ไม่ใช่ root
        _visualApplier = _rigInstance.GetComponentInChildren<IEquipmentVisualApplier>();
        if (_visualApplier == null)
            Debug.LogWarning("[PlayerPreviewUI] Rig ไม่มี IEquipmentVisualApplier — ของสวมจะไม่อัปเดต");
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    private void HandleConnected(EquipmentData data)
    {
        if (_activeData == data) return;
        if (_activeData != null) _activeData.EquippedItems.OnListChanged -= HandleListChanged;

        _activeData = data;
        if (_activeData == null) return;

        _activeData.EquippedItems.OnListChanged += HandleListChanged;
        _needsRefresh = true;
    }

    private void HandleListChanged(NetworkListEvent<NetworkEquipment> evt) => _needsRefresh = true;

    private void LateUpdate()
    {
        if (!_needsRefresh || _activeData == null || _visualApplier == null) return;
        _needsRefresh = false;

        foreach (var entry in _activeData.EquippedItems)
        {
            ItemSO item = entry.ItemID != 0 ? GameDataManager.Instance.itemDatabases.GetItemByID(entry.ItemID) : null;
            _visualApplier.ApplyVisual(entry.Slot, item);
        }
    }
}
