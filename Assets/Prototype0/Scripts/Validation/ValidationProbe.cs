#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace InteractiveShowcase
{
    // Opt-in observer for synthetic-data tests. No images or received coordinates are recorded.
    [DefaultExecutionOrder(1000)]
    public sealed class ValidationProbe : MonoBehaviour
    {
        [Serializable]
        private sealed class Command
        {
            public int id;
            public bool expectedFace;
            public float x;
            public float y;
        }

        [Serializable]
        private sealed class Status
        {
            public int id;
            public int frame;
            public bool face;
            public bool marker;
            public bool geometry;
            public bool positionMatches;
            public bool healthy;
            public int errors;
        }

        private string directory;
        private UdpVisionReceiver receiver;
        private CircleMarker graphic;
        private int errorCount;
        private float nextSample;
        private bool previousRunInBackground;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartFromArguments()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "--vision-validation-dir") Begin(args[i + 1]);
        }

        public static void Begin(string outputDirectory)
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Validation requires Play mode.");
            if (FindFirstObjectByType<ValidationProbe>() != null) throw new InvalidOperationException("Validation is already running.");
            var probe = new GameObject("Synthetic Validation Probe").AddComponent<ValidationProbe>();
            probe.directory = Path.GetFullPath(outputDirectory);
            Directory.CreateDirectory(probe.directory);
            probe.receiver = FindFirstObjectByType<UdpVisionReceiver>();
            probe.graphic = FindFirstObjectByType<CircleMarker>(FindObjectsInactive.Include);
            probe.previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            Application.logMessageReceivedThreaded += probe.OnLog;
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                Interlocked.Increment(ref errorCount);
        }

        private void LateUpdate()
        {
            if (directory == null || Time.realtimeSinceStartup < nextSample) return;
            nextSample = Time.realtimeSinceStartup + 0.1f;
            string path = Path.Combine(directory, "command.json");
            if (!File.Exists(path)) return;
            try
            {
                Command command = JsonUtility.FromJson<Command>(File.ReadAllText(path));
                if (command == null) return;
                bool face = receiver != null && receiver.TryGetFace(out _);
                bool active = graphic != null && graphic.isActiveAndEnabled;
                bool geometry = false;
                bool matches = false;
                if (active)
                {
                    Canvas.ForceUpdateCanvases();
                    var mesh = graphic.canvasRenderer == null ? null : graphic.canvasRenderer.GetMesh();
                    geometry = mesh != null && mesh.vertexCount == 66 && mesh.triangles.Length == 192
                        && graphic.canvasRenderer.materialCount > 0 && graphic.color.a > 0
                        && graphic.canvas != null && graphic.canvas.isActiveAndEnabled;
                    var area = (RectTransform)graphic.transform.parent;
                    // Independent expectation for the saved scene's mirrorX=true / flipY=true.
                    var expected = new Vector2((0.5f - command.x) * area.rect.width,
                        (0.5f - command.y) * area.rect.height);
                    matches = Vector2.Distance(graphic.rectTransform.anchoredPosition, expected) < 2f;
                }
                var status = new Status
                {
                    id = command.id, frame = Time.frameCount, face = face, marker = active,
                    geometry = geometry, positionMatches = matches, errors = errorCount,
                    healthy = errorCount == 0 && receiver != null && graphic != null
                        && (command.expectedFace ? face && active && geometry && matches : !face && !active)
                };
                File.WriteAllText(Path.Combine(directory, "status.json"), JsonUtility.ToJson(status));
            }
            catch (IOException) { /* Another process may be replacing a test command. */ }
        }

        private void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= OnLog;
            Application.runInBackground = previousRunInBackground;
        }
    }
}
#endif
