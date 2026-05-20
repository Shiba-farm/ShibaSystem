using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ItemHoldPreview))]
public class ItemHoldPreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.Space();

        var preview = (ItemHoldPreview)target;
        bool hasInstance = preview.previewInstance != null;

        GUI.enabled = !hasInstance;
        if (GUILayout.Button("▶  Spawn Preview"))
            preview.SpawnPreview();

        GUI.enabled = hasInstance;
        if (GUILayout.Button("✕  Clear Preview"))
            preview.ClearPreview();

        GUI.enabled = true;

        if (hasInstance)
            EditorGUILayout.HelpBox(
                "Adjust holdPositionOffset / holdRotationOffset on the ItemSO to update live.",
                MessageType.Info);
    }
}