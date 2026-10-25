import json
import socket
import sys
import unittest
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from face_detector import FaceDetector, landmark_packet
from udp_sender import UdpSender


class VisionTests(unittest.TestCase):
    def test_center_and_width(self):
        packet = landmark_packet([SimpleNamespace(x=.2, y=.1), SimpleNamespace(x=.8, y=.9)])
        self.assertTrue(packet["faceDetected"])
        self.assertAlmostEqual(packet["x"], .5)
        self.assertAlmostEqual(packet["y"], .5)
        self.assertAlmostEqual(packet["size"], .6)

    def test_clip_frame_edges(self):
        packet = landmark_packet([SimpleNamespace(x=-.2, y=-.1), SimpleNamespace(x=1.2, y=1.3)])
        self.assertEqual(packet, {"faceDetected": True, "x": .5, "y": .5, "size": 1.0})

    def test_invalid_landmarks(self):
        for points in ([], [SimpleNamespace(x=float("nan"), y=.3)],
                       [SimpleNamespace(x=.5, y=.5)],
                       [SimpleNamespace(x=2, y=2), SimpleNamespace(x=3, y=3)]):
            self.assertEqual(landmark_packet(points), {"faceDetected": False})

    def test_udp_payload_and_no_face(self):
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as receiver:
            receiver.bind(("127.0.0.1", 0))
            receiver.settimeout(2)
            sender = UdpSender(receiver.getsockname()[1])
            try:
                for packet in ({"faceDetected": True, "x": .52, "y": .41, "size": .28}, {"faceDetected": False}):
                    self.assertTrue(sender.send(packet))
                    data, address = receiver.recvfrom(1024)
                    self.assertEqual(address[0], "127.0.0.1")
                    self.assertEqual(json.loads(data), packet)
            finally:
                sender.close()

    def test_closed_socket_is_nonfatal(self):
        sender = UdpSender()
        sender.close()
        self.assertFalse(sender.send({"faceDetected": False}))

    def test_real_detector_blank_image(self):
        import numpy as np
        detector = FaceDetector()
        try:
            self.assertEqual(detector.detect(np.zeros((480, 640, 3), dtype=np.uint8)), {"faceDetected": False})
        finally:
            detector.close()

    def test_camera_failure_cleanup(self):
        import main
        with patch.object(sys, "argv", ["main.py"]), patch("cv2.VideoCapture") as capture, \
                patch.object(main, "FaceDetector") as detector, patch.object(main, "UdpSender") as sender:
            capture.return_value.isOpened.return_value = False
            self.assertEqual(main.main(), 1)
            sender.return_value.send.assert_called_with({"faceDetected": False})
            sender.return_value.close.assert_called_once()
            detector.return_value.close.assert_called_once()
            capture.return_value.release.assert_called_once()


if __name__ == "__main__":
    unittest.main()
