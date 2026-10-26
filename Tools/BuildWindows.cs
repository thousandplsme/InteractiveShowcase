// Run through Unity Pipeline's script run, outside Assets to avoid runtime imports.
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class Prototype0WindowsBuild
{
    [Serializable]
    private sealed class Summary
    {
        public string unityVersion;
        public string scene;
        public string output;
        public string result;
        public int errors;
        public int warnings;
        public long sizeBytes;
        public double durationSeconds;
        public bool developmentBuild;
    }

    public static string Build()
    {
        return BuildPlayer(true);
    }

    public static string BuildRelease()
    {
        return BuildPlayer(false);
    }

    private static string BuildPlayer(bool development)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before building.");
        string root = Directory.GetParent(Application.dataPath).FullName;
        string relativeOutput = development ? "Builds/WindowsPrototype0/InteractiveShowcase.exe"
            : "Builds/WindowsPrototype0Release/InteractiveShowcase.exe";
        string output = Path.Combine(root, relativeOutput);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Prototype0/Scenes/Prototype0.unity" },
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            options = development ? BuildOptions.Development : BuildOptions.None
        };
        // Explicit scenes: preserve the user's EditorBuildSettings scene list.
        BuildReport report = BuildPipeline.BuildPlayer(options);
        var summary = new Summary
        {
            unityVersion = Application.unityVersion, scene = options.scenes[0],
            output = relativeOutput,
            result = report.summary.result.ToString(), errors = (int)report.summary.totalErrors,
            warnings = (int)report.summary.totalWarnings, sizeBytes = (long)report.summary.totalSize,
            durationSeconds = report.summary.totalTime.TotalSeconds, developmentBuild = development
        };
        string json = JsonUtility.ToJson(summary, true);
        Directory.CreateDirectory(Path.Combine(root, "TestResults"));
        File.WriteAllText(Path.Combine(root, development ? "TestResults/windows-build.json" : "TestResults/windows-release-build.json"), json);
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException(json);
        // Settings live beside the application, so Python and Unity can share the same file.
        string config = Path.Combine(Path.GetDirectoryName(output), "config");
        Directory.CreateDirectory(config);
        File.Copy(Path.Combine(root, "config/vision.json"), Path.Combine(config, "vision.json"), true);
        return json;
    }
}
