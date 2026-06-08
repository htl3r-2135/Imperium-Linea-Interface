using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    // ------------------------------------------------------------------------
    // Entry Points
    // ------------------------------------------------------------------------

    public static void BuildWindows()
    {
        Build(BuildTarget.StandaloneWindows64);
    }

    public static void BuildLinux()
    {
        Build(BuildTarget.StandaloneLinux64);
    }

    // ------------------------------------------------------------------------
    // Core
    // ------------------------------------------------------------------------

    private static void Build(BuildTarget target)
    {
        try
        {
            Debug.Log("==================================================");
            Debug.Log($"[BuildScript] Starting {target} build");
            Debug.Log("==================================================");

            string outputPath = RequireArg("-buildOutput");

            EnsureOutputDirectory(outputPath);

            string[] scenes = GetEnabledScenes();

            Debug.Log($"[BuildScript] Output Path: {outputPath}");
            Debug.Log($"[BuildScript] Scenes ({scenes.Length})");

            foreach (string scene in scenes)
                Debug.Log($"  - {scene}");

            // Switch active target if necessary
            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                Debug.Log($"[BuildScript] Switching build target to {target}");

                bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildPipeline.GetBuildTargetGroup(target),
                    target
                );

                if (!switched)
                    Fail($"Failed to switch build target to {target}");
            }

            BuildOptions buildOptions = BuildOptions.StrictMode;

            // Optional CLI flags
            if (HasArg("-development"))
                buildOptions |= BuildOptions.Development;

            if (HasArg("-connectProfiler"))
                buildOptions |= BuildOptions.ConnectWithProfiler;

            if (HasArg("-allowDebugging"))
                buildOptions |= BuildOptions.AllowDebugging;

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                options = buildOptions
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log("==================================================");
            Debug.Log($"[BuildScript] Result: {summary.result}");
            Debug.Log($"[BuildScript] Total Errors: {summary.totalErrors}");
            Debug.Log($"[BuildScript] Total Warnings: {summary.totalWarnings}");
            Debug.Log($"[BuildScript] Total Size: {summary.totalSize / 1024f / 1024f:F2} MB");
            Debug.Log($"[BuildScript] Build Time: {summary.totalTime.TotalSeconds:F2}s");
            Debug.Log("==================================================");

            if (summary.result != BuildResult.Succeeded)
                Fail($"Build failed with result: {summary.result}");

            Debug.Log("[BuildScript] Build succeeded.");
            Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Exit(1);
        }
    }

    // ------------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------------

    private static string[] GetEnabledScenes()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            Fail("No enabled scenes found in Build Settings.");

        return scenes;
    }

    private static void EnsureOutputDirectory(string outputPath)
    {
        string directory = Path.GetDirectoryName(outputPath);

        if (string.IsNullOrWhiteSpace(directory))
            Fail($"Invalid output path: {outputPath}");

        Directory.CreateDirectory(directory);
    }

    private static string RequireArg(string argName)
    {
        string value = GetArg(argName);

        if (string.IsNullOrWhiteSpace(value))
            Fail($"Missing required argument: {argName}");

        return value;
    }

    private static string GetArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }

    private static bool HasArg(string name)
    {
        return Environment.GetCommandLineArgs()
            .Any(arg => arg.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private static void Fail(string message)
    {
        Debug.LogError($"[BuildScript] {message}");
        Exit(1);
    }

    private static void Exit(int code)
    {
        Debug.Log($"[BuildScript] Exiting with code {code}");

        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }
}