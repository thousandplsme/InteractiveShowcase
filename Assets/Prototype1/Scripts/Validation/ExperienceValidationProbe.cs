#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Threading;
using TMPro;
using UnityEngine;

namespace InteractiveShowcase.Prototype1
{
    [DefaultExecutionOrder(1100)]
    public sealed class ExperienceValidationProbe : MonoBehaviour
    {
        [Serializable] private sealed class Status
        {
            public int frame, starts, errors;
            public double seconds;
            public string state, reception;
            public bool greeting, marker, green, normal, geometry, positionMatches, textGeometry;
        }
        private string directory;
        private ExperienceController controller;
        private UdpVisionReceiver receiver;
        private CircleMarker marker;
        private TMP_Text greeting;
        private int errors;
        private double nextSample;
        private bool previousBackground;
        private int previousFrameRate;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void FromArguments()
        {
            var args=Environment.GetCommandLineArgs();
            for(int i=0;i+1<args.Length;i++) if(args[i]=="--experience-validation-dir") Begin(args[i+1]);
        }
        public static void Begin(string output)
        {
            if (!Application.isPlaying || FindFirstObjectByType<ExperienceValidationProbe>() != null)
                throw new InvalidOperationException("Probe needs Play mode and must be unique.");
            var probe=new GameObject("Experience Validation Probe").AddComponent<ExperienceValidationProbe>();
            probe.directory=Path.GetFullPath(output); Directory.CreateDirectory(probe.directory);
            probe.controller=FindFirstObjectByType<ExperienceController>();
            probe.receiver=FindFirstObjectByType<UdpVisionReceiver>();
            probe.marker=FindFirstObjectByType<CircleMarker>(FindObjectsInactive.Include);
            foreach(var text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(text.name=="Greeting") probe.greeting=text;
            probe.previousBackground=Application.runInBackground; Application.runInBackground=true;
            probe.previousFrameRate=Application.targetFrameRate; Application.targetFrameRate=60;
            Application.logMessageReceivedThreaded+=probe.Log;
        }
        private void Log(string message,string stack,LogType type)
        { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) Interlocked.Increment(ref errors); }
        private void LateUpdate()
        {
            double now=Time.realtimeSinceStartupAsDouble;
            if(directory==null || now<nextSample) return;
            nextSample=now+.05;
            Canvas.ForceUpdateCanvases();
            bool active=marker!=null && marker.isActiveAndEnabled;
            var mesh=active ? marker.canvasRenderer.GetMesh() : null;
            bool matches=false;
            if(active && receiver.TryGetFace(out var face))
            {
                var rect=((RectTransform)marker.transform.parent).rect;
                var expected=new Vector2((.5f-face.x)*rect.width,(.5f-face.y)*rect.height);
                matches=Vector2.Distance(marker.rectTransform.anchoredPosition,expected)<2;
            }
            bool shown=controller!=null && controller.GreetingVisible;
            if(shown) greeting.ForceMeshUpdate();
            var status=new Status {frame=Time.frameCount,seconds=now,starts=controller.StartCount,
                errors=errors,state=controller.State.ToString(),reception=receiver.State.ToString(),
                greeting=shown,marker=active,green=marker.color==controller.GreetingColor,
                normal=marker.color==controller.NormalColor,geometry=mesh!=null && mesh.vertexCount==66,
                positionMatches=matches,textGeometry=!shown || (greeting.textInfo.characterCount==6 && greeting.textInfo.meshInfo[0].vertexCount>0)};
            try { File.WriteAllText(Path.Combine(directory,"status.json"),JsonUtility.ToJson(status)); }
            catch(IOException) { /* Next sample retries a concurrently read file. */ }
        }
        private void OnDestroy()
        {
            Application.logMessageReceivedThreaded-=Log;
            Application.runInBackground=previousBackground; Application.targetFrameRate=previousFrameRate;
        }
    }
}
#endif
