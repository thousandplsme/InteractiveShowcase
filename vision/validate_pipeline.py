"""Camera-free Python -> UDP -> Unity validation. Requires an opt-in Unity probe.

Reports contain synthetic test names and aggregate results, never received face data.
"""
import argparse
import json
import math
import os
from pathlib import Path
import socket
import subprocess
import sys
import time

from udp_sender import UdpSender


def read_json(path):
    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, ValueError):
        return None


def write_json(path, value):
    temporary = path.with_suffix(".tmp")
    temporary.write_text(json.dumps(value, indent=2), encoding="utf-8")
    for attempt in range(20):
        try:
            os.replace(temporary, path)
            return
        except PermissionError:
            time.sleep(.01)
    raise RuntimeError("Could not replace validation command")


def send_worker(directory, port):
    sender = UdpSender(port)
    raw = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    malformed = {
        "invalid-json": b'{"faceDetected":true,',
        "out-of-range": b'{"faceDetected":true,"x":2,"y":0.5,"size":0.2}',
        "missing-fields": b'{"faceDetected":true}',
        "non-finite": b'{"faceDetected":true,"x":NaN,"y":0.5,"size":0.2}',
        "invalid-utf8": b'\xff\xfe',
        "oversized": b' ' * 2048,
    }
    try:
        while True:
            command = read_json(directory / "command.json")
            if command:
                mode = command["mode"]
                if mode in malformed:
                    raw.sendto(malformed[mode], ("127.0.0.1", port))
                elif mode == "none":
                    sender.send({"faceDetected": False})
                elif mode == "face":
                    sender.send({"faceDetected": True, "x": command["x"],
                                 "y": command["y"], "size": .28})
            time.sleep(1 / 30)
    finally:
        sender.close()
        raw.close()


class PipelineValidation:
    def __init__(self, directory, port):
        self.directory = directory
        self.port = port
        self.worker = None
        self.sequence = 0
        self.results = []

    def start_sender(self):
        self.worker = subprocess.Popen([
            sys.executable, str(Path(__file__).resolve()), "--worker",
            "--directory", str(self.directory), "--port", str(self.port)],
            creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))

    def stop_sender(self):
        if self.worker is not None:
            self.worker.terminate()  # Abrupt process stop: intentionally no final no-face packet.
            self.worker.wait(timeout=5)
            self.worker = None

    def stage(self, name, mode="face", x=.5, y=.5, expected=True, hold=.4):
        self.sequence += 1
        write_json(self.directory / "command.json", dict(
            id=self.sequence, mode=mode, expectedFace=expected, x=x, y=y))
        started = time.monotonic()
        deadline = started + 5
        stable_since = None
        previous_frame = -1
        samples = 0
        while time.monotonic() < deadline:
            if self.worker is not None and self.worker.poll() is not None:
                raise RuntimeError("Synthetic sender exited unexpectedly")
            status = read_json(self.directory / "status.json")
            if status and status.get("id") == self.sequence and status["frame"] != previous_frame:
                previous_frame = status["frame"]
                if status["errors"]:
                    raise RuntimeError("Unity reported an error/assert/exception")
                if status["healthy"]:
                    samples += 1
                    if stable_since is None:
                        stable_since = time.monotonic()
                    if samples >= 3 and time.monotonic() - stable_since >= hold:
                        self.results.append(dict(test=name, passed=True, samples=samples,
                                                 elapsedSeconds=round(time.monotonic() - started, 3)))
                        return
                else:
                    stable_since = None
                    samples = 0
            time.sleep(.025)
        raise RuntimeError("No healthy advancing Unity samples for: " + name)

    def run(self, soak_seconds):
        started = time.monotonic()
        report = dict(syntheticOnly=True, passed=False, checks=self.results)
        try:
            self.stage("initial-hidden", mode="idle", expected=False)
            self.start_sender()
            for name, x, y in [("center", .5, .5), ("top-left", .2, .2),
                               ("top-right", .8, .2), ("bottom-right", .8, .8),
                               ("bottom-left", .2, .8)]:
                self.stage(name, x=x, y=y)
            self.stage("no-face", mode="none", expected=False)
            self.stage("face-return")
            self.stop_sender()
            self.stage("sender-process-stopped", mode="idle", expected=False)
            self.start_sender()
            self.stage("sender-process-restarted", x=.3, y=.7)
            for mode in ("invalid-json", "out-of-range", "missing-fields", "non-finite", "invalid-utf8", "oversized"):
                self.stage(mode, mode=mode, expected=False)
                self.stage("recover-after-" + mode)
            if soak_seconds > 0:
                soak_started = time.monotonic()
                cycle = 0
                while time.monotonic() - soak_started < soak_seconds:
                    # One continuous Python child, changing synthetic positions every ~1.5 seconds.
                    self.stage("soak-" + str(cycle), x=.5 + .3 * math.sin(cycle),
                               y=.5 + .3 * math.cos(cycle), hold=1.0)
                    cycle += 1
                    if cycle % 20 == 0:
                        print(f"Soak: {time.monotonic() - soak_started:.0f}s, {cycle} positions passed", flush=True)
                report["soakSeconds"] = round(time.monotonic() - soak_started, 3)
                report["soakPositions"] = cycle
            self.stop_sender()
            self.stage("final-timeout-hidden", mode="idle", expected=False)
            report["passed"] = True
        except (RuntimeError, OSError, subprocess.SubprocessError) as error:
            report["failure"] = str(error)
        finally:
            self.stop_sender()
            report["elapsedSeconds"] = round(time.monotonic() - started, 3)
            write_json(self.directory / "report.json", report)
        print(json.dumps({key: value for key, value in report.items() if key != "checks"}, indent=2), flush=True)
        print(f"Checks: {len(self.results)}; report: {self.directory / 'report.json'}", flush=True)
        return 0 if report["passed"] else 1


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", type=Path, required=True)
    parser.add_argument("--port", type=int, default=5005)
    parser.add_argument("--soak-seconds", type=float, default=0)
    parser.add_argument("--worker", action="store_true", help=argparse.SUPPRESS)
    args = parser.parse_args()
    if not 1 <= args.port <= 65535 or not math.isfinite(args.soak_seconds) or args.soak_seconds < 0:
        parser.error("Invalid port or soak duration")
    directory = args.directory.resolve()
    directory.mkdir(parents=True, exist_ok=True)
    if args.worker:
        send_worker(directory, args.port)
        return 0
    return PipelineValidation(directory, args.port).run(args.soak_seconds)


if __name__ == "__main__":
    raise SystemExit(main())
