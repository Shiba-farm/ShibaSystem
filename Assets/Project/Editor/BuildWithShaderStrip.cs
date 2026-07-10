using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Strips S_Rock shader variants at build time so the DX11 sampler limit error never fires.
/// Use Build → Build And Strip S_Rock instead of the normal Build And Run.
/// </summary>
public class BuildWithShaderStrip : IPreprocessShaders
{
    // IPreprocessShaders — runs before every shader variant is compiled into the build
    public int callbackOrder => 0;

    private static readonly string[] ShadersToStrip = { "Shader Graphs/S_Rock" };

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        foreach (var name in ShadersToStrip)
        {
            if (shader.name == name)
            {
                Debug.Log($"[BuildStrip] Stripping all {data.Count} variants of '{shader.name}' to avoid ps_4_0 sampler limit.");
                data.Clear();   // removes every variant of this shader from the build
                return;
            }
        }
    }
}

/// <summary>
/// Menu item that kicks off a normal build — the IPreprocessShaders above runs automatically.
/// </summary>
public class ForceBuild
{
    [MenuItem("Build/Build And Strip S_Rock")]
    public static void Build()
    {
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes       = GetEnabledScenes(),
            locationPathName = "Builds/ForceBuild/Game.exe",
            target       = BuildTarget.StandaloneWindows64,
            options      = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"[ForceBuild] Result: {report.summary.result}  |  Errors: {report.summary.totalErrors}");
    }

    [MenuItem("Build/Build And Run And Strip S_Rock")]
    public static void BuildAndRun()
    {
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes       = GetEnabledScenes(),
            locationPathName = "Builds/ForceBuild/Game.exe",
            target       = BuildTarget.StandaloneWindows64,
            options      = BuildOptions.AutoRunPlayer
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"[ForceBuild] Result: {report.summary.result}  |  Errors: {report.summary.totalErrors}");
    }

    static string[] GetEnabledScenes()
    {
        var scenes = new System.Collections.Generic.List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
            if (scene.enabled) scenes.Add(scene.path);
        return scenes.ToArray();
    }
}
