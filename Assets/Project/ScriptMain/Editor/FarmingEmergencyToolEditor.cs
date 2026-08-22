using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FarmingEmergencyTool))]
public class FarmingEmergencyToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var tool = (FarmingEmergencyTool)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Manual Controls", EditorStyles.boldLabel);

        if (GUILayout.Button("Force Show Cursor (Ignore Category)"))
            tool.ForceShowCursor();

        if (GUILayout.Button("Simulate Holding Test Seed"))
            tool.SimulateHoldingSeed();

        if (GUILayout.Button("Add Tilled Cell at Current Mouse Position"))
            tool.TillAtMouse();

        if (GUILayout.Button("Check System Status"))
            tool.CheckSystemStatus();
    }
}
