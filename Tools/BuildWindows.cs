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
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before building.");
        string root = Directory.GetParent(Application.dataPath).FullName;
        string output = Path.Combine(root, "Builds/WindowsPrototype0/InteractiveShowcase.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Prototype0/Scenes/Prototype0.unity" },
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        };
        // Explicit scenes: preserve the user's EditorBuildSettings scene list.
        BuildReport report = BuildPipeline.BuildPlayer(options);
        var summary = new Summary
        {
            unityVersion = Application.unityVersion, scene = options.scenes[0],
            output = "Builds/WindowsPrototype0/InteractiveShowcase.exe",
            result = report.summary.result.ToString(), errors = (int)report.summary.totalErrors,
            warnings = (int)report.summary.totalWarnings, sizeBytes = (long)report.summary.totalSize,
            durationSeconds = report.summary.totalTime.TotalSeconds, developmentBuild = true
        };
        string json = JsonUtility.ToJson(summary, true);
        Directory.CreateDirectory(Path.Combine(root, "TestResults"));
        File.WriteAllText(Path.Combine(root, "TestResults/windows-build.json"), json);
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException(json);
        return json;
    }
}
