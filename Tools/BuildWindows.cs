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
        public int prototype;
    }

    public static string Build()
    {
        return BuildPlayer(true);
    }

    public static string BuildRelease()
    {
        return BuildPlayer(false);
    }

    public static string BuildPrototype1() => BuildPlayer(true, 1);
    public static string BuildPrototype1Release() => BuildPlayer(false, 1);

    private static string BuildPlayer(bool development, int prototype = 0)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before building.");
        string root = Directory.GetParent(Application.dataPath).FullName;
        string relativeOutput = "Builds/WindowsPrototype" + prototype + (development ? "" : "Release") + "/InteractiveShowcase.exe";
        string output = Path.Combine(root, relativeOutput);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Prototype" + prototype + "/Scenes/Prototype" + prototype + ".unity" },
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
            durationSeconds = report.summary.totalTime.TotalSeconds, developmentBuild = development, prototype = prototype
        };
        string json = JsonUtility.ToJson(summary, true);
        Directory.CreateDirectory(Path.Combine(root, "TestResults"));
        string reportDirectory = prototype == 0 ? "TestResults" : "TestResults/prototype1";
        Directory.CreateDirectory(Path.Combine(root, reportDirectory));
        File.WriteAllText(Path.Combine(root, reportDirectory, development ? "windows-build.json" : "windows-release-build.json"), json);
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException(json);
        // Settings live beside the application, so Python and Unity can share the same file.
        string config = Path.Combine(Path.GetDirectoryName(output), "config");
        Directory.CreateDirectory(config);
        File.Copy(Path.Combine(root, "config/vision.json"), Path.Combine(config, "vision.json"), true);
        if (prototype == 1)
        {
            File.Copy(Path.Combine(root, "config/experience.json"), Path.Combine(config, "experience.json"), true);
            string licenses = Path.Combine(Path.GetDirectoryName(output), "Licenses");
            Directory.CreateDirectory(licenses);
            File.Copy(Path.Combine(root, "Assets/Prototype1/Fonts/OFL.txt"), Path.Combine(licenses, "MPLUS1p-OFL.txt"), true);
            foreach (string path in Directory.GetFiles(Path.Combine(root, "Assets/TextMesh Pro"), "*.txt", SearchOption.AllDirectories))
                File.Copy(path, Path.Combine(licenses, Path.GetFileName(path)), true);
        }
        return json;
    }
}
