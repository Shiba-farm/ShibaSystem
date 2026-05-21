#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// แก้ warning: "TerrainCollider: MeshCollider is not supported on terrain at the moment."
/// Unity Terrain รองรับแค่ CapsuleCollider บน tree prototypes เท่านั้น
/// Script นี้จะ:
///   Option A - ปิด Tree Colliders ที่ Terrain (เร็วสุด)
///   Option B - แทน MeshCollider ด้วย CapsuleCollider ใน tree prototypes ทุกตัว
/// </summary>
public class FixTreeColliders
{
    // ─── Option A: ปิด Tree Colliders ที่ Terrain ────────────────────────────
    [MenuItem("Tools/Fix Tree Colliders/Option A - Disable Tree Colliders on Terrain")]
    static void DisableTreeColliders()
    {
        Terrain[] terrains = Object.FindObjectsOfType<Terrain>();
        if (terrains.Length == 0)
        {
            Debug.LogWarning("[FixTreeColliders] ไม่พบ Terrain ใน Scene");
            return;
        }

        foreach (var terrain in terrains)
        {
            TerrainCollider tc = terrain.GetComponent<TerrainCollider>();
            if (tc == null) continue;

            // enableTreeColliders ไม่มี public property ใน Unity 2022
            // ต้องใช้ SerializedObject เข้าถึง serialized field แทน
            SerializedObject so = new SerializedObject(tc);
            SerializedProperty prop = so.FindProperty("m_EnableTreeColliders");
            if (prop != null && prop.boolValue)
            {
                prop.boolValue = false;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(tc);
                Debug.Log($"[FixTreeColliders] ปิด Tree Colliders บน: {terrain.gameObject.name}");
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("[FixTreeColliders] Option A เสร็จแล้ว — กด Ctrl+S เซฟ Scene");
    }

    // ─── Option B: แทน MeshCollider → CapsuleCollider ใน tree prototypes ─────
    [MenuItem("Tools/Fix Tree Colliders/Option B - Replace MeshCollider with CapsuleCollider in Tree Prefabs")]
    static void ReplaceMeshCollidersInTreePrototypes()
    {
        Terrain[] terrains = Object.FindObjectsOfType<Terrain>();
        if (terrains.Length == 0)
        {
            Debug.LogWarning("[FixTreeColliders] ไม่พบ Terrain ใน Scene");
            return;
        }

        int fixedCount = 0;
        int skippedCount = 0;

        foreach (var terrain in terrains)
        {
            if (terrain.terrainData == null) continue;

            TreePrototype[] prototypes = terrain.terrainData.treePrototypes;
            Debug.Log($"[FixTreeColliders] Terrain '{terrain.name}' มี {prototypes.Length} tree prototypes");

            foreach (var proto in prototypes)
            {
                if (proto.prefab == null) continue;

                string prefabPath = AssetDatabase.GetAssetPath(proto.prefab);
                if (string.IsNullOrEmpty(prefabPath)) continue;

                // โหลด prefab root
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                if (prefabRoot == null) continue;

                bool modified = false;

                // ตรวจสอบ MeshCollider บน root และ children ทั้งหมด
                MeshCollider[] meshCols = prefabRoot.GetComponentsInChildren<MeshCollider>(true);
                foreach (var mc in meshCols)
                {
                    // ข้ามถ้าไม่ได้อยู่บน root (Terrain ดู collider ที่ root เท่านั้น)
                    if (mc.gameObject != prefabRoot) continue;

                    // เพิ่ม CapsuleCollider แทน ถ้ายังไม่มี
                    if (prefabRoot.GetComponent<CapsuleCollider>() == null)
                    {
                        CapsuleCollider cap = prefabRoot.AddComponent<CapsuleCollider>();

                        // ประมาณขนาดจาก MeshCollider bounds
                        if (mc.sharedMesh != null)
                        {
                            Bounds b = mc.sharedMesh.bounds;
                            cap.height = b.size.y;
                            cap.radius = Mathf.Max(b.size.x, b.size.z) * 0.25f;
                            cap.center = b.center;
                        }
                        else
                        {
                            cap.height = 3f;
                            cap.radius = 0.5f;
                        }
                    }

                    // ลบ MeshCollider บน root
                    Object.DestroyImmediate(mc);
                    modified = true;
                    fixedCount++;
                    Debug.Log($"[FixTreeColliders] แก้แล้ว: {prefabRoot.name} → แทน MeshCollider ด้วย CapsuleCollider");
                }

                if (modified)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                }
                else
                {
                    skippedCount++;
                }

                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[FixTreeColliders] Option B เสร็จแล้ว — แก้ {fixedCount} prefabs, ข้าม {skippedCount} prefabs");
        EditorUtility.DisplayDialog(
            "Fix Tree Colliders เสร็จแล้ว",
            $"แก้ไข {fixedCount} tree prefabs\nข้าม {skippedCount} prefabs (ไม่มี MeshCollider บน root)\n\nกด Ctrl+S เซฟ Scene",
            "OK"
        );
    }
}
#endif
