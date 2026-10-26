using System;
using TMPro;
using UnityEngine;

namespace InteractiveShowcase.Prototype1
{
    public sealed class ExperienceController : MonoBehaviour
    {
        [SerializeField] private UdpVisionReceiver receiver;
        [SerializeField] private CircleMarker marker;
        [SerializeField] private TMP_Text greeting;
        [SerializeField] private Color normalColor = new Color(.25f, .9f, .8f, 1);
        [SerializeField] private Color greetingColor = new Color(.25f, 1f, .35f, 1);
        private ExperienceStateMachine machine;
        public ExperienceState State => machine?.State ?? ExperienceState.Offline;
        public int StartCount => machine?.StartCount ?? 0;
        public bool GreetingVisible => greeting != null && greeting.gameObject.activeSelf;
        public bool SettingsValid { get; private set; }
        public Color NormalColor => normalColor;
        public Color GreetingColor => greetingColor;

        public void Configure(UdpVisionReceiver source, CircleMarker circle, TMP_Text text)
        {
            receiver = source; marker = circle; greeting = text;
        }

        private void OnEnable() => Initialize();

        // An explicit path allows isolated configuration-error tests without modifying production settings.
        public void Initialize(string settingsPath = null)
        {
            machine = null; SettingsValid = false; Render(false);
            try
            {
                ExperienceSettings settings = ExperienceSettings.Load(settingsPath);
                machine = new ExperienceStateMachine(settings.detectionHoldSeconds, settings.greetingSeconds, settings.exitHoldSeconds);
                SettingsValid = true;
            }
            catch (Exception error) when (error is System.IO.IOException || error is UnauthorizedAccessException || error is FormatException)
            {
                Debug.LogError("Prototype 1 settings unavailable: " + error.GetType().Name, this);
            }
        }

        public static bool IsConnected(UdpVisionReceiver.ReceptionState state) =>
            state == UdpVisionReceiver.ReceptionState.Tracking || state == UdpVisionReceiver.ReceptionState.NoFace;

        private void LateUpdate()
        {
            if (!SettingsValid) return;
            var state = receiver != null ? receiver.State : UdpVisionReceiver.ReceptionState.Stopped;
            machine.Tick(Time.realtimeSinceStartupAsDouble, IsConnected(state), state == UdpVisionReceiver.ReceptionState.Tracking);
            Render(machine.IsGreeting);
        }

        private void Render(bool visible)
        {
            if (greeting != null) greeting.gameObject.SetActive(visible);
            if (marker != null) marker.color = visible ? greetingColor : normalColor;
        }

        private void OnDisable()
        {
            machine?.Reset();
            Render(false);
        }
    }
}
