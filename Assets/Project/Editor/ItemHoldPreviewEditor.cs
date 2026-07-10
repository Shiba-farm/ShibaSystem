using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ItemHoldPreview))]
public class ItemHoldPreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "ItemHoldPreview is superseded by the Equipment Preview window.\n" +
            "All offset editing, animation preview, and live updates now live there.",
            MessageType.Info);

        EditorGUILayout.Space(4);

        if (GUILayout.Button("⚔  Open Equipment Preview Window", GUILayout.Height(28)))
            EquipmentPreviewWindow.Open();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Legacy Controls", EditorStyles.boldLabel);

        base.OnInspectorGUI();

        EditorGUILayout.Space();
        var preview = (ItemHoldPreview)target;
        bool hasInstance = preview.previewInstance != null;

        GUI.enabled = !hasInstance;
        if (GUILayout.Button("▶  Spawn Preview (Legacy)"))
            preview.SpawnPreview();

        GUI.enabled = hasInstance;
        if (GUILayout.Button("✕  Clear Preview (Legacy)"))
            preview.ClearPreview();

        GUI.enabled = true;
    }
}
