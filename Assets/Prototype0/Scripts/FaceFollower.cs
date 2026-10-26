using UnityEngine;

namespace InteractiveShowcase
{
    public sealed class FaceFollower : MonoBehaviour
    {
        [SerializeField, Tooltip("Read config/vision.json at startup. Disable for Inspector overrides.")]
        private bool useSharedSettings = true;
        [SerializeField] private UdpVisionReceiver receiver;
        [SerializeField] private RectTransform area;
        [SerializeField] private RectTransform marker;
        [SerializeField] private bool mirrorX = true;
        [SerializeField] private bool flipY = true;
        [SerializeField, Min(0.001f)] private float smoothTime = 0.08f;
        private Vector2 velocity;
        private bool visible;

        private void OnEnable()
        {
            if (!useSharedSettings) return;
            try
            {
                VisionSettings settings = VisionSettings.Load();
                mirrorX = settings.mirrorX; flipY = settings.flipY; smoothTime = settings.smoothTime;
            }
            catch (System.Exception error) when (error is System.IO.IOException || error is System.UnauthorizedAccessException
                || error is System.FormatException || error is System.ArgumentException)
            {
                Debug.LogError("FaceFollower settings error: " + error.Message, this);
                enabled = false;
            }
        }

        public void Configure(UdpVisionReceiver source, RectTransform bounds, RectTransform target)
        {
            receiver = source; area = bounds; marker = target;
        }

        public static Vector2 MapPosition(Rect rect, float x, float y, bool mirror, bool invertY)
        {
            return new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, mirror ? 1 - x : x),
                Mathf.Lerp(rect.yMin, rect.yMax, invertY ? 1 - y : y));
        }

        private void LateUpdate()
        {
            if (marker == null) return;
            bool detected = receiver != null && area != null && receiver.TryGetFace(out _);
            if (detected)
            {
                receiver.TryGetFace(out UdpVisionReceiver.VisionPacket face);
                Vector2 target = MapPosition(area.rect, face.x, face.y, mirrorX, flipY);
                // Center anchors: local rect coordinates equal anchoredPosition for centered area pivot.
                target -= area.rect.center;
                marker.anchoredPosition = visible
                    ? Vector2.SmoothDamp(marker.anchoredPosition, target, ref velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime)
                    : target;
            }
            else velocity = Vector2.zero;
            marker.gameObject.SetActive(detected);
            visible = detected;
        }

        private void OnDisable()
        {
            visible = false;
            velocity = Vector2.zero;
            if (marker != null) marker.gameObject.SetActive(false);
        }
    }
}
