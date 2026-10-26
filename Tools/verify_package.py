"""Check the distribution ZIP, file manifest, release-only assembly and source exclusion."""
import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import zipfile


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archive", type=Path)
    args = parser.parse_args()
    archive = args.archive.resolve()
    with archive.open("rb") as stream:
        checksum = hashlib.file_digest(stream, "sha256").hexdigest()
    expected = archive.with_suffix(".zip.sha256").read_text(encoding="ascii").split()[0]
    if checksum != expected:
        raise RuntimeError("ZIP checksum mismatch")
    with zipfile.ZipFile(archive) as package:
        prefix = archive.stem + "/"
        names = package.namelist()
        if len(names) != len(set(names)) or any(not name.startswith(prefix) or ".." in PurePosixPath(name).parts for name in names):
            raise RuntimeError("Unexpected archive paths")
        if package.testzip() is not None:
            raise RuntimeError("ZIP CRC failure")
        manifest = json.loads(package.read(prefix + "manifest.json"))
        if manifest["developmentBuild"]:
            raise RuntimeError("Expected release package")
        expected_names = set(manifest["files"]) | {"manifest.json"}
        if {name[len(prefix):] for name in names} != expected_names:
            raise RuntimeError("Manifest does not cover the full package")
        forbidden = {".git", ".venv", "__pycache__", "library", "testresults", "captures", "recordings", "face-data"}
        for relative, expected_hash in manifest["files"].items():
            if any(part.lower() in forbidden for part in PurePosixPath(relative).parts) or relative.endswith((".pdb", ".log")):
                raise RuntimeError("Excluded data in package: " + relative)
            with package.open(prefix + relative) as stream:
                actual = hashlib.file_digest(stream, "sha256").hexdigest()
            if actual != expected_hash:
                raise RuntimeError("File checksum mismatch: " + relative)
        assembly = package.read(prefix + "InteractiveShowcase_Data/Managed/InteractiveShowcase.Runtime.dll")
        if b"ValidationProbe" in assembly:
            raise RuntimeError("Development validation probe present")
        if manifest.get("prototype", 0) == 1:
            p1 = package.read(prefix + "InteractiveShowcase_Data/Managed/InteractiveShowcase.Prototype1.dll")
            if b"ExperienceValidationProbe" in p1:
                raise RuntimeError("P1 development probe present")
            for required in ("StartPrototype1.cmd", "config/experience.json", "Licenses/MPLUS1p-OFL.txt"):
                if required not in expected_names:
                    raise RuntimeError("P1 required file absent: " + required)
        elif "config/experience.json" in expected_names:
            raise RuntimeError("P1 settings mixed into P0 package")
        for required in ("InteractiveShowcase.exe", "UnityPlayer.dll", "StartShowcase.cmd", "SetupVision.cmd",
                         "CheckEnvironment.cmd", "vision/main.py", "vision/requirements-lock.txt", "config/vision.json", "README.md"):
            if required not in expected_names:
                raise RuntimeError("Required file absent: " + required)
    print(json.dumps(dict(passed=True, version=manifest["version"], sourceCommit=manifest["sourceCommit"],
                          files=len(names), sha256=checksum, bytes=archive.stat().st_size), indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
