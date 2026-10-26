"""Run from repository root: python vision/main.py --camera 0."""
import argparse
import sys
import time

from face_detector import FaceDetector
from udp_sender import UdpSender
from settings import DEFAULT_PATH, load_settings


def main():
    parser = argparse.ArgumentParser(description="Local single-face tracking; Ctrl+C to stop")
    parser.add_argument("--config", default=str(DEFAULT_PATH), help="Shared settings JSON")
    parser.add_argument("--camera", type=int, help="Override configured OpenCV camera index")
    parser.add_argument("--port", type=int, help="Override configured UDP port")
    parser.add_argument("--preview", action="store_true", help="Show transient, unmirrored camera view; Q to stop")
    args = parser.parse_args()
    try:
        settings = load_settings(args.config)
    except (OSError, ValueError) as error:
        parser.error("Cannot load settings: " + str(error))
    args.camera = settings["cameraIndex"] if args.camera is None else args.camera
    args.port = settings["port"] if args.port is None else args.port
    if args.camera < 0 or not 1 <= args.port <= 65535:
        parser.error("camera must be >= 0; port must be 1..65535")

    import cv2
    capture = None
    detector = None
    sender = UdpSender(args.port)
    exit_code = 0
    try:
        detector = FaceDetector()
        capture = cv2.VideoCapture(args.camera)
        if not capture.isOpened():
            raise RuntimeError("Camera unavailable: check --camera, Windows permission and other apps.")
        capture.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
        capture.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)
        capture.set(cv2.CAP_PROP_FPS, 30)
        print("Tracking locally. No images saved. Ctrl+C to stop.")
        warned = False
        while True:
            started = time.monotonic()
            ok, frame = capture.read()
            if not ok:
                raise RuntimeError("Camera read failed. Reconnect the camera and restart this command.")
            packet = detector.detect(frame)
            sent = sender.send(packet)
            if not sent and not warned:
                print("UDP send failed; retrying on subsequent frames.", file=sys.stderr)
            warned = not sent
            if args.preview:
                if packet["faceDetected"]:
                    h, w = frame.shape[:2]
                    cv2.circle(frame, (int(packet["x"] * w), int(packet["y"] * h)), 12, (0, 255, 0), 2)
                cv2.imshow("Prototype 0 - unmirrored (Q to stop)", frame)
                if cv2.waitKey(1) & 0xFF == ord("q"):
                    break
            time.sleep(max(0, 1 / 30 - (time.monotonic() - started)))
    except KeyboardInterrupt:
        pass
    except (RuntimeError, ValueError, OSError, cv2.error) as error:
        print(str(error), file=sys.stderr)
        exit_code = 1
    finally:
        sender.send({"faceDetected": False})
        sender.close()
        if capture is not None:
            capture.release()
        if detector is not None:
            detector.close()
        if args.preview:
            cv2.destroyAllWindows()
    return exit_code


if __name__ == "__main__":
    raise SystemExit(main())
