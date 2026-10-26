using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace InteractiveShowcase.Tests
{
    public class VisionTests
    {
        [Test]
        public void AddingCircleMarkerIncludesCanvasRenderer()
        {
            var marker = new GameObject("Circle renderer test", typeof(RectTransform));
            try
            {
                marker.AddComponent<CircleMarker>();
                Assert.IsNotNull(marker.GetComponent<CanvasRenderer>(), "A Graphic without CanvasRenderer cannot draw.");
            }
            finally { Object.DestroyImmediate(marker); }
        }

        [Test]
        public void SavedSceneMarkerHasCanvasRenderer()
        {
            const string path = "Assets/Prototype0/Scenes/Prototype0.unity";
            // Inspect the saved asset in isolation, including when the user's scene is unsaved.
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(path);
            try
            {
                CircleMarker marker = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    marker = root.GetComponentInChildren<CircleMarker>(true);
                    if (marker != null) break;
                }
                Assert.IsNotNull(marker);
                Assert.IsNotNull(marker.GetComponent<CanvasRenderer>());
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [TestCase("{\"faceDetected\":true,\"x\":0.52,\"y\":0.41,\"size\":0.28}", true)]
        [TestCase("{\"faceDetected\":false}", true)]
        [TestCase("{\"faceDetected\":true,\"x\":0.52,\"y\":0.41}", false)]
        [TestCase("{\"faceDetected\":true,\"x\":2,\"y\":0.41,\"size\":0.28}", false)]
        [TestCase("{\"faceDetected\":true,\"x\":0,\"y\":0,\"size\":0}", false)]
        [TestCase("{}", false)]
        [TestCase("garbage", false)]
        [TestCase("{\"faceDetected\":\"true\"}", false)]
        public void ValidatesPackets(string json, bool expected)
        {
            Assert.AreEqual(expected, UdpVisionReceiver.TryParse(json, out _));
        }

        [Test]
        public void MapsImageCoordinatesAndMirroring()
        {
            var rect = new Rect(-960, -540, 1920, 1080);
            Assert.AreEqual(new Vector2(-960, 540), FaceFollower.MapPosition(rect, 0, 0, false, true));
            Assert.AreEqual(new Vector2(960, -540), FaceFollower.MapPosition(rect, 0, 1, true, true));
            Assert.AreEqual(Vector2.zero, FaceFollower.MapPosition(rect, .5f, .5f, true, true));
            Assert.AreEqual(new Vector2(-960, -540), FaceFollower.MapPosition(rect, 0, 0, false, false));
        }

        [UnityTest]
        public IEnumerator UdpTimeoutRecoveryAndSafeRebind()
        {
            yield return new EnterPlayMode();
            var root = new GameObject("Receiver test");
            root.SetActive(false);
            var receiver = root.AddComponent<UdpVisionReceiver>();
            var follower = root.AddComponent<FaceFollower>();
            var area = new GameObject("Area", typeof(RectTransform));
            var bounds = area.GetComponent<RectTransform>();
            bounds.sizeDelta = new Vector2(1920, 1080);
            var marker = new GameObject("Marker", typeof(RectTransform));
            var markerRect = marker.GetComponent<RectTransform>();
            markerRect.SetParent(bounds, false);
            follower.Configure(receiver, bounds, markerRect);
            // Reserve a free ephemeral test port; production 5005 stays untouched.
            int port;
            using (var reservation = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
                port = ((IPEndPoint)reservation.Client.LocalEndPoint).Port;
            var serialized = new UnityEditor.SerializedObject(receiver);
            serialized.FindProperty("useSharedSettings").boolValue = false;
            serialized.FindProperty("port").intValue = port;
            serialized.FindProperty("timeoutSeconds").floatValue = .15f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var followerSettings = new UnityEditor.SerializedObject(follower);
            followerSettings.FindProperty("useSharedSettings").boolValue = false;
            followerSettings.ApplyModifiedPropertiesWithoutUndo();
            using (var sender = new UdpClient())
            {
                try
                {
                    root.SetActive(true);
                    Assert.IsFalse(receiver.TryGetFace(out _));
                    Assert.AreEqual(UdpVisionReceiver.ReceptionState.Waiting, receiver.State);
                    for (int cycle = 0; cycle < 2; cycle++)
                    {
                        byte[] face = Encoding.UTF8.GetBytes("{\"faceDetected\":true,\"x\":0.2,\"y\":0.8,\"size\":0.3}");
                        double end = UnityEditor.EditorApplication.timeSinceStartup + 2;
                        while (!receiver.TryGetFace(out _) && UnityEditor.EditorApplication.timeSinceStartup < end)
                        {
                            sender.Send(face, face.Length, "127.0.0.1", port);
                            yield return null;
                            receiver.SendMessage("Update");
                        }
                        Assert.IsTrue(receiver.TryGetFace(out var received), "UDP did not arrive");
                        Assert.AreEqual(UdpVisionReceiver.ReceptionState.Tracking, receiver.State);
                        Assert.AreEqual(.2f, received.x);
                        follower.SendMessage("LateUpdate");
                        Assert.IsTrue(marker.activeSelf);
                        Assert.That(markerRect.anchoredPosition.x, Is.EqualTo(576f).Within(.01f));
                        Assert.That(markerRect.anchoredPosition.y, Is.EqualTo(-324f).Within(.01f));
                        // Malformed data must not refresh the valid-packet deadline.
                        double waitUntil = UnityEditor.EditorApplication.timeSinceStartup + .25;
                        byte[] invalid = Encoding.UTF8.GetBytes("invalid");
                        while (UnityEditor.EditorApplication.timeSinceStartup < waitUntil)
                        {
                            sender.Send(invalid, invalid.Length, "127.0.0.1", port);
                            yield return null;
                            receiver.SendMessage("Update");
                        }
                        Assert.IsFalse(receiver.TryGetFace(out _), "Stale face remained visible");
                        Assert.AreEqual(UdpVisionReceiver.ReceptionState.TimedOut, receiver.State);
                        follower.SendMessage("LateUpdate");
                        Assert.IsFalse(marker.activeSelf);
                        root.SetActive(false);
                        Assert.IsFalse(receiver.TryGetFace(out _));
                        root.SetActive(true);
                    }
                    byte[] noFace = Encoding.UTF8.GetBytes("{\"faceDetected\":false}");
                    double noFaceDeadline = UnityEditor.EditorApplication.timeSinceStartup + 2;
                    UdpVisionReceiver.VisionPacket absent;
                    do
                    {
                        sender.Send(noFace, noFace.Length, "127.0.0.1", port);
                        yield return null;
                        receiver.SendMessage("Update");
                        receiver.TryGetFace(out absent);
                    } while (absent == null && UnityEditor.EditorApplication.timeSinceStartup < noFaceDeadline);
                    Assert.IsNotNull(absent);
                    Assert.IsFalse(absent.faceDetected);
                    Assert.AreEqual(UdpVisionReceiver.ReceptionState.NoFace, receiver.State);
                }
                finally { Object.DestroyImmediate(root); Object.DestroyImmediate(area); }
            }
            using (var rebound = new UdpClient(new IPEndPoint(IPAddress.Loopback, port)))
                Assert.IsNotNull(rebound);
        }

        [UnityTearDown]
        public IEnumerator LeavePlayMode()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
    }
}
