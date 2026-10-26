using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace InteractiveShowcase.Prototype1.Tests
{
    public sealed class SceneTests
    {
        const string ScenePath="Assets/Prototype1/Scenes/Prototype1.unity";
        const string Face="{\"faceDetected\":true,\"x\":0.25,\"y\":0.75,\"size\":0.3}";
        const string Absent="{\"faceDetected\":false}";

        [Test]
        public void U01_U08_SavedSceneReferencesFontAndLayout()
        {
            var scene=EditorSceneManager.OpenPreviewScene(ScenePath);
            try
            {
                int controllers=0,texts=0,markers=0;
                foreach(var root in scene.GetRootGameObjects())
                {
                    controllers+=root.GetComponentsInChildren<ExperienceController>(true).Length;
                    foreach(var marker in root.GetComponentsInChildren<CircleMarker>(true))
                    {markers++;Assert.NotNull(marker.GetComponent<CanvasRenderer>());}
                    foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
                    {
                        texts++;Assert.NotNull(text.font);Assert.NotNull(text.font.material.shader);
                        Assert.True(text.font.HasCharacters(text.text,out uint[] missing,false,false),text.name);
                        var r=text.rectTransform;
                        Assert.That(r.anchorMin.x,Is.GreaterThanOrEqualTo(0));Assert.That(r.anchorMax.x,Is.LessThanOrEqualTo(1));
                        Assert.That(r.anchorMin.y,Is.GreaterThanOrEqualTo(0));Assert.That(r.anchorMax.y,Is.LessThanOrEqualTo(1));
                        foreach(var size in new[]{new Vector2(1920,1080),new Vector2(1366,768),new Vector2(1280,800)})
                        {
                            Vector2 region=Vector2.Scale(r.anchorMax-r.anchorMin,size);
                            Assert.Greater(region.x,0);Assert.Greater(region.y,0);
                            float scale=Mathf.Sqrt(size.x/1920f*size.y/1080f);
                            Vector2 preferred=text.GetPreferredValues()*scale;
                            Assert.LessOrEqual(preferred.x,region.x,text.name+" overflows width");
                            Assert.LessOrEqual(preferred.y,region.y,text.name+" overflows height");
                        }
                    }
                }
                Assert.AreEqual(1,controllers);Assert.AreEqual(3,texts);Assert.AreEqual(1,markers);
            }
            finally {EditorSceneManager.ClosePreviewScene(scene);}
        }

        private IEnumerator Pump(UdpClient sender,int port,double seconds,string data)
        {
            double end=Time.realtimeSinceStartupAsDouble+seconds;
            byte[] bytes=Encoding.UTF8.GetBytes(data);
            do {sender.Send(bytes,bytes.Length,"127.0.0.1",port);yield return null;}
            while(Time.realtimeSinceStartupAsDouble<end);
            yield return null;
        }

        [UnityTest]
        public IEnumerator U02_U07_C04_RealSceneLifecycle()
        {
            yield return new EnterPlayMode();
            foreach(var previous in Object.FindObjectsByType<UdpVisionReceiver>(FindObjectsSortMode.None)) previous.enabled=false;
            EditorSceneManager.LoadSceneInPlayMode(ScenePath,new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var receiver=Object.FindFirstObjectByType<UdpVisionReceiver>();
            var controller=Object.FindFirstObjectByType<ExperienceController>();
            var marker=Object.FindFirstObjectByType<CircleMarker>(FindObjectsInactive.Include);
            receiver.enabled=false;
            int port;
            using(var reservation=new UdpClient(new IPEndPoint(IPAddress.Loopback,0))) port=((IPEndPoint)reservation.Client.LocalEndPoint).Port;
            var serialized=new SerializedObject(receiver);
            serialized.FindProperty("useSharedSettings").boolValue=false;
            serialized.FindProperty("port").intValue=port;
            serialized.FindProperty("timeoutSeconds").floatValue=.15f;
            serialized.ApplyModifiedPropertiesWithoutUndo();receiver.enabled=true;
            Directory.CreateDirectory("TestResults/prototype1");
            string config=Path.GetFullPath("TestResults/prototype1/test-experience.json");
            File.WriteAllText(config,"{\"detectionHoldSeconds\":0.1,\"greetingSeconds\":1,\"exitHoldSeconds\":0.1}");
            controller.Initialize(config);
            bool background=Application.runInBackground;
            float scale=Time.timeScale;
            Application.runInBackground=true;
            using(var sender=new UdpClient())
            {
                try
                {
                    Time.timeScale=0;
                    yield return Pump(sender,port,.25,Face);
                    Assert.True(controller.GreetingVisible);Assert.True(marker.isActiveAndEnabled);Assert.AreEqual(controller.GreetingColor,marker.color);
                    yield return Pump(sender,port,.2,Absent);
                    Assert.False(marker.isActiveAndEnabled);Assert.True(controller.GreetingVisible);
                    yield return Pump(sender,port,.2,Face);
                    Assert.True(marker.isActiveAndEnabled);Assert.True(controller.GreetingVisible);Assert.AreEqual(1,controller.StartCount);
                    yield return Pump(sender,port,.8,Face);
                    Assert.False(controller.GreetingVisible);Assert.AreEqual(controller.NormalColor,marker.color);Assert.AreEqual(1,controller.StartCount);
                    yield return Pump(sender,port,.2,Absent);
                    yield return Pump(sender,port,.25,Face);
                    Assert.True(controller.GreetingVisible);Assert.AreEqual(2,controller.StartCount);
                    receiver.enabled=false;yield return null;yield return null;
                    Assert.False(controller.GreetingVisible);Assert.False(marker.isActiveAndEnabled);
                    receiver.enabled=true;yield return Pump(sender,port,.25,Face);
                    Assert.True(controller.GreetingVisible);
                    controller.enabled=false;yield return null;
                    Assert.False(controller.GreetingVisible);Assert.AreEqual(controller.NormalColor,marker.color);
                    controller.enabled=true;controller.Initialize(config);
                    Assert.Zero(controller.StartCount);Assert.False(controller.GreetingVisible);
                    LogAssert.Expect(LogType.Error,"Prototype 1 settings unavailable: FileNotFoundException");
                    controller.Initialize(config+".missing");
                    yield return Pump(sender,port,.3,Face);
                    Assert.False(controller.SettingsValid);Assert.False(controller.GreetingVisible);Assert.True(marker.isActiveAndEnabled);
                    using(var locked=new FileStream(config,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
                    {
                        LogAssert.Expect(LogType.Error,"Prototype 1 settings unavailable: IOException");controller.Initialize(config);
                        Assert.False(controller.SettingsValid);
                    }
                    controller.Initialize(config);yield return Pump(sender,port,.25,Face);Assert.True(controller.GreetingVisible);
                    // A real bind failure must be mapped to Offline, not an ordinary exit.
                    receiver.enabled=false;
                    using(var occupied=new UdpClient(new IPEndPoint(IPAddress.Loopback,port)))
                    {
                        LogAssert.Expect(LogType.Error,new System.Text.RegularExpressions.Regex("Vision UDP could not start"));
                        receiver.enabled=true;yield return null;yield return null;
                        Assert.AreEqual(UdpVisionReceiver.ReceptionState.Error,receiver.State);
                        Assert.AreEqual(ExperienceState.Offline,controller.State);Assert.False(controller.GreetingVisible);
                        receiver.enabled=false;
                    }
                    receiver.enabled=true;yield return Pump(sender,port,.25,Face);Assert.True(controller.GreetingVisible);
                    Time.timeScale=scale;
                    EditorSceneManager.LoadSceneInPlayMode(ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
                    var reloaded=Object.FindFirstObjectByType<ExperienceController>();
                    Assert.Zero(reloaded.StartCount);Assert.False(reloaded.GreetingVisible);
                }
                finally {Time.timeScale=scale;Application.runInBackground=background;}
            }
        }

        [UnityTearDown] public IEnumerator LeavePlay(){if(Application.isPlaying)yield return new ExitPlayMode();}
    }
}
