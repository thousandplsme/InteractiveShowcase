using UnityEditor;
using UnityEngine;

namespace InteractiveShowcase.Editor
{
    [CustomEditor(typeof(UdpVisionReceiver))]
    public sealed class UdpVisionReceiverEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var receiver = (UdpVisionReceiver)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Reception state", receiver.State.ToString());
            EditorGUILayout.LabelField("Local UDP port", receiver.Port.ToString());
            EditorGUILayout.HelpBox("Waiting: no valid packet yet. NoFace: connected, no face. TimedOut: valid data stopped. Settings changes require restart.", MessageType.Info);
            if (Application.isPlaying) Repaint();
        }
    }
}
