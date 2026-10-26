"""Read-only startup checks. Does not open the camera or send any UDP data."""
import argparse
import importlib
from importlib.metadata import PackageNotFoundError, version
from pathlib import Path
import socket
import struct
import sys

from settings import DEFAULT_PATH, load_settings


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--config", type=Path, default=DEFAULT_PATH)
    parser.add_argument("--no-pause", action="store_true", help=argparse.SUPPRESS)
    args = parser.parse_args()
    failed = False
    if sys.version_info[:2] != (3, 11) or struct.calcsize("P") != 8 or sys.prefix == sys.base_prefix:
        print("FAIL: Use a dedicated Python 3.11 x64 venv.")
        failed = True
    else:
        print("OK: Python 3.11 x64 venv")
    for line in Path(__file__).with_name("requirements-lock.txt").read_text().splitlines():
        if not line.strip() or line.startswith("#"):
            continue
        name, expected = line.split("==")
        try:
            if version(name) != expected:
                print(f"FAIL: {name} differs from requirements-lock.txt")
                failed = True
        except PackageNotFoundError:
            print(f"FAIL: {name} is missing")
            failed = True
    for module in ("cv2", "mediapipe", "numpy"):
        try:
            importlib.import_module(module)
            print(f"OK: {module} import")
        except (ImportError, OSError) as error:
            print(f"FAIL: {module} import ({type(error).__name__})")
            failed = True
    try:
        settings = load_settings(args.config)
        print(f"OK: settings; camera index {settings['cameraIndex']}, localhost UDP {settings['port']}")
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as probe:
            if hasattr(socket, "SO_EXCLUSIVEADDRUSE"):
                probe.setsockopt(socket.SOL_SOCKET, socket.SO_EXCLUSIVEADDRUSE, 1)
            try:
                probe.bind(("127.0.0.1", settings["port"]))
                print("INFO: UDP port is free; start Unity before checking marker movement.")
            except OSError:
                print("INFO: UDP port unavailable. Expected if Unity is running; otherwise check for duplicate receivers.")
    except (OSError, ValueError) as error:
        print("FAIL: settings cannot be used (" + str(error) + ")")
        failed = True
    print("Camera access/face detection are NOT tested. No camera was opened and no packets were sent.")
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
