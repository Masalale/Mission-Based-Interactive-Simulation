using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Builds the game. Use the menu (Build > Build WebGL, Build > Build Android APK)
// or the command line. Each build uses the scenes that are enabled in Build Settings
// and prints one "[Build]" line with the result, so it is easy to find in a log.
public static class BuildScript
{
    // Where the finished builds go. Both folders are inside the project.
    private const string WebGLFolder = "Builds/WebGL";
    private const string AndroidFile = "Builds/Android/BigFiveHeritageSafari.apk";

    [MenuItem("Build/Build WebGL")]
    public static void BuildWebGL()
    {
        // GitHub Pages cannot serve compressed WebGL files (.gz or .br), so turn compression off.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

        // The game is designed for a 1280 x 720 screen.
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;

        Directory.CreateDirectory(WebGLFolder);
        Run(BuildTargetGroup.WebGL, BuildTarget.WebGL, WebGLFolder, "WebGL");
    }

    [MenuItem("Build/Build Android APK")]
    public static void BuildAndroid()
    {
        // Build an APK file, not an Android App Bundle (.aab).
        EditorUserBuildSettings.buildAppBundle = false;

        Directory.CreateDirectory(Path.GetDirectoryName(AndroidFile));
        Run(BuildTargetGroup.Android, BuildTarget.Android, AndroidFile, "Android");
    }

    // Does the actual work for both platforms.
    private static void Run(BuildTargetGroup group, BuildTarget target, string output, string label)
    {
        // The build only works for the platform that is active, so switch first.
        // If it is already active, this does nothing.
        if (EditorUserBuildSettings.activeBuildTarget != target)
            EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);

        // Collect the scenes that are turned on in Build Settings.
        var scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
                scenes.Add(scene.path);
        }

        BuildReport report = BuildPipeline.BuildPlayer(scenes.ToArray(), output, target, BuildOptions.None);
        BuildSummary summary = report.summary;

        Debug.Log($"[Build] {label} result={summary.result} size={summary.totalSize} errors={summary.totalErrors}");

        // When the build runs from the command line, exit with 0 for success and 1 for failure.
        if (Application.isBatchMode)
            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
