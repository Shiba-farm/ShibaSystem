#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class FixTerrainData : MonoBehaviour
{
    [MenuItem("Tools/Fix Terrain Data")]
    static void Fix()
    {
        Terrain terrain = FindObjectOfType<Terrain>();
        TerrainCollider col = terrain.GetComponent<TerrainCollider>();
        col.terrainData = terrain.terrainData;
        EditorUtility.SetDirty(col);
        Debug.Log("Fixed! TerrainData: " + terrain.terrainData.name);
    }
}
#endif