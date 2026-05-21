#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class AssignTerrain : MonoBehaviour
{
    [MenuItem("Tools/Assign ShibaScene")]
    static void Assign()
    {
        Terrain terrain = FindObjectOfType<Terrain>();
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>
            ("Assets/!ShibaFarm/ShibaScene.asset");
        terrain.terrainData = data;
        terrain.GetComponent<TerrainCollider>().terrainData = data;
        EditorUtility.SetDirty(terrain);
        Debug.Log("Done: " + data.name);
    }
}
#endif