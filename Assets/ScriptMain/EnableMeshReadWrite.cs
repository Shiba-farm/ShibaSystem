#if UNITY_EDITOR
using UnityEditor;

public class EnableMeshReadWrite : EditorWindow
{
    [MenuItem("Tools/Enable Read-Write on All Meshes")]
    static void EnableAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                count++;
            }
        }

        UnityEngine.Debug.Log($"[MeshFix] Enabled Read/Write on {count} models.");
    }
}
#endif