using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace InteractiveShowcase
{
    public sealed class UdpVisionReceiver : MonoBehaviour
    {
        [SerializeField, Tooltip("Read config/vision.json at startup. Disable only for isolated tests or Inspector overrides.")]
        private bool useSharedSettings = true;
        [SerializeField] private int port = 5005;
        [SerializeField, Min(0.1f)] private float timeoutSeconds = 0.5f;
        private readonly object gate = new object();
        private UdpClient client;
        private Thread worker;
        private volatile bool running;
        private string pending;
        private double pendingTime;
        private double lastValidTime = double.NegativeInfinity;
        private VisionPacket latest;
        private string failure;
        private bool faulted;

        public enum ReceptionState { Stopped, Waiting, NoFace, Tracking, TimedOut, Error }
        public ReceptionState State => faulted ? ReceptionState.Error : !running ? ReceptionState.Stopped
            : latest == null ? ReceptionState.Waiting : Now - lastValidTime > timeoutSeconds ? ReceptionState.TimedOut
            : latest.faceDetected ? ReceptionState.Tracking : ReceptionState.NoFace;
        public int Port => port;

        [Serializable]
        public sealed class VisionPacket
        {
            public bool faceDetected;
            public float x = float.NaN;
            public float y = float.NaN;
            public float size = float.NaN;
        }

        private static double Now => (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency;

        public bool TryGetFace(out VisionPacket packet)
        {
            packet = latest;
            return running && packet != null && packet.faceDetected && Now - lastValidTime <= timeoutSeconds;
        }

        private void OnEnable()
        {
            latest = null;
            faulted = false;
            lastValidTime = double.NegativeInfinity;
            lock (gate) { pending = null; failure = null; }
            try
            {
                if (useSharedSettings)
                {
                    VisionSettings settings = VisionSettings.Load();
                    port = settings.port;
                    timeoutSeconds = settings.timeoutSeconds;
                }
                client = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
                client.Client.ReceiveTimeout = 100;
                running = true;
                // Capture the socket so an old worker can never read a replacement socket.
                UdpClient socket = client;
                worker = new Thread(() => ReceiveLoop(socket)) { IsBackground = true, Name = "Vision UDP" };
                worker.Start();
            }
            catch (Exception error) when (error is SocketException || error is ArgumentException
                || error is System.IO.IOException || error is UnauthorizedAccessException || error is FormatException)
            {
                faulted = true;
                running = false;
                client?.Close();
                client = null;
                UnityEngine.Debug.LogError("Vision UDP could not start. Check config/vision.json and port: " + error.Message, this);
            }
        }

        private void ReceiveLoop(UdpClient socket)
        {
            var endpoint = new IPEndPoint(IPAddress.Any, 0);
            var utf8 = new UTF8Encoding(false, true);
            while (running)
            {
                try
                {
                    byte[] bytes = socket.Receive(ref endpoint);
                    if (!IPAddress.IsLoopback(endpoint.Address) || bytes.Length > 1024) continue;
                    string json = utf8.GetString(bytes);
                    lock (gate) { pending = json; pendingTime = Now; }
                }
                catch (SocketException error)
                {
                    if (!running) break;
                    if (error.SocketErrorCode == SocketError.TimedOut) continue;
                    lock (gate) failure = "Vision UDP receive failed: " + error.SocketErrorCode;
                    running = false;
                }
                catch (ObjectDisposedException) { break; }
                catch (DecoderFallbackException) { /* Ignore malformed UTF-8. */ }
            }
        }

        private void Update()
        {
            string json;
            string error;
            double receivedAt;
            lock (gate)
            {
                json = pending; receivedAt = pendingTime; pending = null;
                error = failure; failure = null;
            }
            if (error != null) { faulted = true; UnityEngine.Debug.LogError(error, this); }
            if (json != null && TryParse(json, out VisionPacket packet))
            {
                latest = packet;
                lastValidTime = receivedAt;
            }
        }

        public static bool TryParse(string json, out VisionPacket packet)
        {
            packet = null;
            if (string.IsNullOrWhiteSpace(json) || json.Length > 1024) return false;
            string trimmed = json.Trim();
            if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}")) return false;
            // JsonUtility ignores unknown fields. Require the protocol discriminator.
            if (!System.Text.RegularExpressions.Regex.IsMatch(trimmed, "\"faceDetected\"\\s*:\\s*(true|false)\\s*[,}]")) return false;
            try
            {
                var value = new VisionPacket();
                JsonUtility.FromJsonOverwrite(trimmed, value);
                if (value.faceDetected && (!Unit(value.x) || !Unit(value.y) || !Unit(value.size) || value.size <= 0)) return false;
                packet = value;
                return true;
            }
            catch (ArgumentException) { return false; }
        }

        private static bool Unit(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= 1;

        private void OnDisable()
        {
            running = false;
            client?.Close(); // Unblocks Receive without waiting for a packet.
            worker?.Join(250);
            worker = null;
            client = null;
            latest = null;
            lock (gate) pending = null;
        }
    }
}
