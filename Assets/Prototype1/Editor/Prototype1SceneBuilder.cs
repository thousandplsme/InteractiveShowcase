using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InteractiveShowcase.Prototype1.Editor
{
    public static class Prototype1SceneBuilder
    {
        public const string ScenePath = "Assets/Prototype1/Scenes/Prototype1.unity";
        public const string FontPath = "Assets/Prototype1/Fonts/MPLUS1p SDF.asset";
        public const string Instruction = "顔を向けると反応するよ！";
        public const string Greeting = "こんにちは！";
        public const string Privacy = "カメラで顔の位置を検出しています。映像は保存しません。";

        public static void Create()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first.");
            if (File.Exists(ScenePath)) throw new System.InvalidOperationException("P1 scene already exists; not overwritten.");
            Directory.CreateDirectory("Assets/Prototype1/Scenes");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Prototype1/Fonts/MPLUS1p-Regular.ttf");
                font = TMP_FontAsset.CreateFontAsset(source, 90, 8, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
                font.name = "MPLUS1p SDF";
                AssetDatabase.CreateAsset(font, FontPath);
                font.atlasTextures[0].name = "MPLUS1p Atlas";
                AssetDatabase.AddObjectToAsset(font.atlasTextures[0], font);
                font.material.name = "MPLUS1p Material";
                AssetDatabase.AddObjectToAsset(font.material, font);
                if (!font.TryAddCharacters(Instruction + Greeting + Privacy, out string missing))
                    throw new System.InvalidOperationException("Missing font characters: " + missing);
                font.atlasPopulationMode = AtlasPopulationMode.Static;
                EditorUtility.SetDirty(font);
                AssetDatabase.SaveAssets();
            }
            // Copy the authored P0 scene through Unity, preserving its GUID and original contents.
            if (!AssetDatabase.CopyAsset("Assets/Prototype0/Scenes/Prototype0.unity", ScenePath))
                throw new System.IO.IOException("Could not copy scene.");
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                UdpVisionReceiver receiver = null;
                CircleMarker marker = null;
                Canvas canvas = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    receiver = receiver ?? root.GetComponentInChildren<UdpVisionReceiver>(true);
                    marker = marker ?? root.GetComponentInChildren<CircleMarker>(true);
                    canvas = canvas ?? root.GetComponentInChildren<Canvas>(true);
                }
                if (receiver == null || marker == null || canvas == null) throw new System.InvalidOperationException("P0 scene references missing.");
                MakeText(canvas.transform, font, "Instruction", Instruction, 64, new Vector2(.05f, .82f), new Vector2(.95f, .96f));
                var text = MakeText(canvas.transform, font, "Greeting", Greeting, 128, new Vector2(.08f, .38f), new Vector2(.92f, .64f));
                MakeText(canvas.transform, font, "Privacy", Privacy, 30, new Vector2(.05f, .04f), new Vector2(.95f, .14f));
                text.gameObject.SetActive(false);
                var controller = receiver.gameObject.AddComponent<ExperienceController>();
                controller.Configure(receiver, marker, text);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                SceneManager.SetActiveScene(previous);
            }
            AssetDatabase.SaveAssets();
        }

        private static TMP_Text MakeText(Transform parent, TMP_FontAsset font, string name, string value, int size, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.text = value;
            text.color = Color.white; text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }
    }
}
