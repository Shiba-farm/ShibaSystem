using UnityEngine;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// วางสคริปต์นี้ไว้บน GameObject ว่างๆ ในซีน (เป็นโหนดแม่ของแปลง)
public class SoilGridSpawner : MonoBehaviour
{
    [Title("ขนาดกริด")]
    [BoxGroup("Grid Settings")]
    [LabelText("จำนวนแถว (rows)"), MinValue(1)]
    public int rows = 2;

    [BoxGroup("Grid Settings")]
    [LabelText("จำนวนคอลัมน์ (cols)"), MinValue(1)]
    public int cols = 4;

    [BoxGroup("Grid Settings")]
    [LabelText("ระยะห่างแนวหน้า-หลัง (เมตร)"), MinValue(0.1f)]
    public float rowSpacing = 1f;   // ระยะห่างแนวหน้า-หลัง

    [BoxGroup("Grid Settings")]
    [LabelText("ระยะห่างแนวซ้าย-ขวา (เมตร)"), MinValue(0.1f)]
    public float colSpacing = 1f;   // ระยะห่างแนวซ้าย-ขวา

    [Title("Raycast ลงพื้น")]
    [BoxGroup("Raycast")]
    [LabelText("Ground Mask")]
    public LayerMask groundMask;    // เลือก Terrain/พื้นของคุณ

    [BoxGroup("Raycast"), LabelText("ความสูงเริ่มยิง Ray"), MinValue(0.1f)]
    public float raycastHeight = 2f; // ยิงจากเหนือจุดขึ้นไป

    [Title("Prefab / การปลูก")]
    [BoxGroup("Prefab")]
    [Required, LabelText("Prefab แปลงดิน (ต้องมี SoilTile + Collider)")]
    public GameObject soilTilePrefab; // ต้องมีคอมโพเนนต์ SoilTile + Collider

    [BoxGroup("Prefab")]
    [LabelText("ตั้ง Layer = Soil อัตโนมัติ")]
    public bool setLayerToSoil = true;

    [BoxGroup("Prefab")]
    [LabelText("ชื่อ Layer ของดิน")]
    public string soilLayerName = "Soil";

    [BoxGroup("Auto Plant")]
    [LabelText("ปลูกอัตโนมัติหลังสปอว์น")]
    public bool autoPlant = false;

    [BoxGroup("Auto Plant")]
    [ShowIf("autoPlant")]
    [LabelText("พืชที่จะปลูก")]
    public CropSO plantThisCrop;     // พืชที่จะปลูกอัตโนมัติหลังสปอว์น

    [BoxGroup("Auto Plant")]
    [ShowIf("autoPlant")]
    [LabelText("รดน้ำให้ด้วยทันที")]
    public bool waterOnPlant = false; // ถ้าติ๊กจะรดน้ำให้ด้วย

    // ====== ปุ่มทำงานใน Inspector ======

    [Button("ล้างลูกทั้งหมด")]
    [GUIColor(1f, 0.5f, 0.5f)]
    public void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(c.gameObject);
            else Destroy(c.gameObject);
#else
            Destroy(c.gameObject);
#endif
        }
    }

    [Button("Generate Grid")]
    [GUIColor(0.5f, 1f, 0.5f)]
    public void GenerateGrid()
    {
        if (!soilTilePrefab)
        {
            Debug.LogError("[SoilGridSpawner] กรุณากำหนด soilTilePrefab");
            return;
        }

        ClearChildren();

        // จัดให้อยู่กลางกริด
        Vector3 right = transform.right * colSpacing;
        Vector3 fwd = transform.forward * rowSpacing;
        Vector3 originOffset = -right * (cols - 1) / 2f - fwd * (rows - 1) / 2f;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector3 localOffset = originOffset + right * c + fwd * r;
                Vector3 worldPosTop = transform.TransformPoint(localOffset + Vector3.up * raycastHeight);

                // ยิงลงพื้น
                if (Physics.Raycast(worldPosTop, Vector3.down, out RaycastHit hit, raycastHeight * 3f, groundMask))
                {
                    // วางดิน
                    GameObject go = Instantiate(soilTilePrefab, hit.point, Quaternion.identity, transform);

                    // ให้ดินหันขึ้นตามพื้น (ถ้าอยากชิดพื้น slope)
                    go.transform.up = hit.normal;

                    // ตั้ง Layer = Soil (ถ้าต้อง)
                    if (setLayerToSoil && !string.IsNullOrEmpty(soilLayerName))
                        SetLayerRecursive(go, LayerMask.NameToLayer(soilLayerName));

                    // ปลูกอัตโนมัติถ้าต้องการ
                    SoilTile tile = go.GetComponent<SoilTile>();
                    if (tile != null && autoPlant && plantThisCrop != null)
                    {
                        tile.Till();              // ไถก่อน
                        tile.Plant(plantThisCrop);
                        if (waterOnPlant) tile.Water();
                    }
                }
            }
        }
    }

    private void SetLayerRecursive(GameObject go, int layer)
    {
        if (layer < 0) return;
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursive(t.gameObject, layer);
    }
}
