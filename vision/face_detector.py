"""Single-face coordinates; no recording, identity or pose estimation."""
import math


def landmark_packet(landmarks):
    """Bounding-box center in unmirrored image coordinates, origin top-left.

    size is the box width divided by image width. Only the 468 mesh points
    are used (iris refinement is unnecessary). Out-of-frame values are clipped.
    """
    points = list(landmarks)[:468]
    if not points or any(not math.isfinite(p.x) or not math.isfinite(p.y) for p in points):
        return {"faceDetected": False}
    left = max(0.0, min(1.0, min(p.x for p in points)))
    right = max(0.0, min(1.0, max(p.x for p in points)))
    top = max(0.0, min(1.0, min(p.y for p in points)))
    bottom = max(0.0, min(1.0, max(p.y for p in points)))
    if right <= left or bottom <= top:
        return {"faceDetected": False}
    return {"faceDetected": True, "x": (left + right) / 2,
            "y": (top + bottom) / 2, "size": right - left}


class FaceDetector:
    def __init__(self):
        import cv2
        from mediapipe import solutions
        self._cv2 = cv2
        self._mesh = solutions.face_mesh.FaceMesh(
            static_image_mode=False, max_num_faces=1, refine_landmarks=False,
            min_detection_confidence=0.5, min_tracking_confidence=0.5)

    def detect(self, frame):
        rgb = self._cv2.cvtColor(frame, self._cv2.COLOR_BGR2RGB)
        rgb.flags.writeable = False
        result = self._mesh.process(rgb)
        if not result.multi_face_landmarks:
            return {"faceDetected": False}
        return landmark_packet(result.multi_face_landmarks[0].landmark)

    def close(self):
        self._mesh.close()
