"""P1 synthetic end-to-end scenarios; never opens a camera. Uses the real UDP sender."""
import argparse
import json
from pathlib import Path
import subprocess
import sys
import time

from validate_pipeline import read_json, write_json


class ExperienceValidation:
    def __init__(self, directory, port):
        self.directory, self.port = directory, port
        self.worker = None
        self.results = []
        self.starts = 0

    def start(self):
        self.worker = subprocess.Popen([sys.executable, str(Path(__file__).with_name("validate_pipeline.py")),
            "--worker", "--directory", str(self.directory), "--port", str(self.port)],
            creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))

    def stop(self):
        if self.worker is not None:
            self.worker.terminate()
            self.worker.wait(timeout=5)
            self.worker = None

    def send(self, mode):
        write_json(self.directory / "command.json", dict(mode=mode, x=.25, y=.75))

    def observe(self, name, state, greeting, marker, hold=.12, deadline=5):
        began = time.monotonic()
        stable = None
        frame = -1
        samples = 0
        while time.monotonic() - began < deadline:
            if self.worker is not None and self.worker.poll() is not None:
                raise RuntimeError("Sender exited unexpectedly")
            status = read_json(self.directory / "status.json")
            if status and status["frame"] != frame:
                frame = status["frame"]
                if status["errors"]:
                    raise RuntimeError("Unity error/assert/exception")
                healthy = (status["state"] == state and status["greeting"] == greeting
                    and status["marker"] == marker and status["starts"] == self.starts
                    and status["textGeometry"] and (status["green"] if greeting else status["normal"])
                    and (not marker or (status["geometry"] and status["positionMatches"])))
                if healthy:
                    if stable is None:
                        stable = time.monotonic()
                    samples += 1
                    if samples >= 2 and time.monotonic() - stable >= hold:
                        self.results.append(dict(test=name, passed=True, starts=self.starts, samples=samples,
                                                 elapsedSeconds=round(time.monotonic()-began, 3)))
                        return status
                else:
                    stable, samples = None, 0
            time.sleep(.025)
        raise RuntimeError("No advancing healthy samples: " + name)

    def begin_greeting(self, name):
        self.send("face")
        self.starts += 1
        return self.observe(name, "Greeting", True, True)

    def run(self, soak):
        report = dict(syntheticOnly=True, passed=False, checks=self.results)
        began = time.monotonic()
        try:
            self.observe("initial-offline", "Offline", False, False)
            self.send("none"); self.start()
            self.observe("connected-no-face", "Ready", False, False)
            self.begin_greeting("E01-first-greeting")
            self.observe("E01-no-repeat", "AwaitExit", False, True, hold=.4)
            self.send("none"); self.observe("E03-short-exit", "AwaitExit", False, False)
            self.send("face"); self.observe("E03-short-return", "AwaitExit", False, True)
            self.send("none"); self.observe("E04-full-exit", "Ready", False, False)
            self.begin_greeting("E04-second-greeting")
            self.send("none"); self.observe("E03-greeting-exit", "Greeting", True, False)
            self.send("face"); self.observe("E03-greeting-return", "Greeting", True, True)
            self.observe("E03-original-deadline", "AwaitExit", False, True)
            self.send("none"); self.observe("prepare-full-exit", "Ready", False, False)
            self.begin_greeting("E02-third-greeting")
            self.send("none"); self.observe("E02-finish-text-despite-exit", "Greeting", True, False, hold=1.1)
            self.observe("E02-rearm-after-text", "Ready", False, False)
            self.begin_greeting("E05-before-kill")
            self.stop(); self.observe("E05-killed-sender", "Offline", False, False)
            self.start(); self.starts += 1
            self.observe("E06-restart", "Greeting", True, True)
            for mode in ("invalid-json", "out-of-range", "missing-fields", "non-finite", "invalid-utf8", "oversized"):
                self.send(mode); self.observe("E07-" + mode, "Offline", False, False)
                self.begin_greeting("E07-recover-" + mode)
            self.send("none"); self.observe("E08-last-no-face", "Greeting", True, False)
            self.stop(); self.observe("E08-no-face-then-silence", "Offline", False, False)
            self.send("none"); self.start(); self.observe("soak-ready", "Ready", False, False)
            soak_started = time.monotonic()
            cycles = 0
            while time.monotonic()-soak_started < soak:
                self.begin_greeting("E09-start-" + str(cycles))
                self.send("none"); self.observe("E09-exit-" + str(cycles), "Greeting", True, False)
                self.observe("E09-ready-" + str(cycles), "Ready", False, False)
                cycles += 1
                if cycles % 10 == 0:
                    print(f"P1 soak: {time.monotonic()-soak_started:.0f}s, {cycles} cycles", flush=True)
            report.update(soakSeconds=round(time.monotonic()-soak_started,3), soakCycles=cycles)
            self.stop(); self.observe("final-offline", "Offline", False, False)
            report["passed"] = True
        except (RuntimeError, OSError, subprocess.SubprocessError) as error:
            report["failure"] = str(error)
        finally:
            self.stop()
            report.update(elapsedSeconds=round(time.monotonic()-began,3), expectedStarts=self.starts)
            write_json(self.directory / "report.json", report)
        print(json.dumps({k:v for k,v in report.items() if k != "checks"}, indent=2), flush=True)
        print("Checks:", len(self.results), flush=True)
        return 0 if report["passed"] else 1


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", type=Path, required=True)
    parser.add_argument("--port", type=int, default=5006)
    parser.add_argument("--soak-seconds", type=int, default=0)
    args = parser.parse_args()
    if not 1 <= args.port <= 65535 or args.soak_seconds < 0:
        parser.error("Invalid port or duration")
    args.directory.mkdir(parents=True, exist_ok=True)
    return ExperienceValidation(args.directory.resolve(), args.port).run(args.soak_seconds)


if __name__ == "__main__":
    raise SystemExit(main())
