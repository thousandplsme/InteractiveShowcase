using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InteractiveShowcase.Editor
{
    public static class Prototype0SceneBuilder
    {
        public const string ScenePath = "Assets/Prototype0/Scenes/Prototype0.unity";

        public static void CreateBatch()
        {
            // Batch mode starts with an untitled scene which cannot host additive creation.
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            Create();
        }

        [MenuItem("InteractiveShowcase/Create Prototype 0 Scene")]
        public static void Create()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Debug.Log("Prototype 0 already exists: " + ScenePath);
                return;
            }
            System.IO.Directory.CreateDirectory("Assets/Prototype0/Scenes");
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            // Overlay UI still needs an active camera to avoid Game View's
            // "No cameras rendering" message. Render only a solid background.
            var cameraObject = new GameObject("Background Camera", typeof(Camera),
                typeof(UnityEngine.Rendering.Universal.UniversalAdditionalCameraData));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.055f, 1);
            camera.cullingMask = 0;
            camera.orthographic = true;
            camera.transform.position = new Vector3(0, 0, -10);
            var root = new GameObject("Vision Tracking");
            var receiver = root.AddComponent<UdpVisionReceiver>();
            var follower = root.AddComponent<FaceFollower>();
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var background = new GameObject("Background", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.SetParent(canvasObject.transform, false);
            backgroundRect.anchorMin = Vector2.zero; backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero; backgroundRect.offsetMax = Vector2.zero;
            var backdrop = background.GetComponent<UnityEngine.UI.Image>();
            backdrop.color = new Color(0.025f, 0.035f, 0.055f, 1);
            backdrop.raycastTarget = false;
            var marker = new GameObject("Face Marker", typeof(RectTransform), typeof(CanvasRenderer), typeof(CircleMarker));
            var markerRect = marker.GetComponent<RectTransform>();
            markerRect.SetParent(canvasObject.transform, false);
            markerRect.anchorMin = markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.sizeDelta = new Vector2(80, 80);
            var graphic = marker.GetComponent<CircleMarker>();
            graphic.color = new Color(0.25f, 0.9f, 0.8f, 1);
            graphic.raycastTarget = false;
            marker.SetActive(false);
            follower.Configure(receiver, canvasObject.GetComponent<RectTransform>(), markerRect);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.CloseScene(scene, true);
            SceneManager.SetActiveScene(previous);
            AssetDatabase.Refresh();
            Debug.Log("Created " + ScenePath);
        }
    }
}
