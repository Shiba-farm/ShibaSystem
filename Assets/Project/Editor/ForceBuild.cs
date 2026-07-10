// using UnityEditor;
// using UnityEditor.Build.Reporting;
// using UnityEngine;

// public class ForceBuild
// {
//     [MenuItem("Build/Force Build (Ignore All)")]
//     public static void Build()
//     {
//         BuildPlayerOptions options = new BuildPlayerOptions
//         {
//             scenes = GetAllScenes(),
//             locationPathName = "Builds/ForceBuild/Game.exe",
//             target = BuildTarget.StandaloneWindows64,
//             options = BuildOptions.None
//         };

//         // ปิด error pause
//         PlayerSettings.suppressUnityWarnings = true;

//         BuildReport report = BuildPipeline.BuildPlayer(options);
//         Debug.Log($"Build result: {report.summary.result}");
//     }

//     static string[] GetAllScenes()
//     {
//         var scenes = new System.Collections.Generic.List<string>();
//         foreach (var scene in EditorBuildSettings.scenes)
//             if (scene.enabled) scenes.Add(scene.path);
//         return scenes.ToArray();
//     }
// }