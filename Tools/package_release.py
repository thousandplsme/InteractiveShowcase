"""Create a local distribution from the release build; generated files stay outside Git."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parents[1]


def digest(path):
    hasher = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            hasher.update(chunk)
    return hasher.hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--version", required=True)
    parser.add_argument("--prototype", type=int, choices=(0, 1), default=0)
    args = parser.parse_args()
    if not re.fullmatch(r"[0-9]+\.[0-9]+\.[0-9]+(?:-[a-z0-9.]+)?", args.version):
        parser.error("Use a version such as 0.1.0")
    build = ROOT / "Builds" / f"WindowsPrototype{args.prototype}Release"
    assembly = build / "InteractiveShowcase_Data" / "Managed" / "InteractiveShowcase.Runtime.dll"
    if not (build / "InteractiveShowcase.exe").is_file() or not assembly.is_file():
        parser.error("Build the Windows release first")
    if b"ValidationProbe" in assembly.read_bytes():
        parser.error("ValidationProbe must not be present in the release assembly")
    report_dir = ROOT / "TestResults" / ("prototype1" if args.prototype else "")
    summary = json.loads((report_dir / "windows-release-build.json").read_text(encoding="utf-8-sig"))
    if summary.get("prototype", 0) != args.prototype:
        parser.error("Build report prototype mismatch")
    if args.prototype:
        p1_assembly = build / "InteractiveShowcase_Data/Managed/InteractiveShowcase.Prototype1.dll"
        if not p1_assembly.is_file() or b"ExperienceValidationProbe" in p1_assembly.read_bytes():
            parser.error("P1 runtime missing or contains development probe")
        if digest(ROOT / "config/experience.json") != digest(build / "config/experience.json"):
            parser.error("Build experience settings differ")
    if summary["result"] != "Succeeded" or summary["developmentBuild"]:
        parser.error("A successful non-development build report is required")
    if digest(ROOT / "config/vision.json") != digest(build / "config/vision.json"):
        parser.error("Build and repository settings differ; rebuild before packaging")
    package = ROOT / "dist" / ("InteractiveShowcase-" + args.version)
    archive = package.parent / (package.name + ".zip")
    if package.exists() or archive.exists():
        parser.error("Version output already exists; no files were overwritten")
    revision = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
    if subprocess.check_output(["git", "status", "--porcelain"], cwd=ROOT, text=True).strip():
        parser.error("Commit the audited sources before packaging")
    package.mkdir(parents=True)
    # Exclude Unity's generated debug/backup directories, symbols and local logs.
    for source in build.iterdir():
        if source.name.endswith(("_BurstDebugInformation_DoNotShip", "_BackUpThisFolder_ButDontShipItWithYourGame")):
            continue
        if source.is_dir():
            shutil.copytree(source, package / source.name, ignore=shutil.ignore_patterns("*.pdb", "*.log"))
        elif source.suffix not in (".pdb", ".log"):
            shutil.copy2(source, package / source.name)
    for name in ("StartShowcase.cmd", "StartVision.cmd", "StartVisionPreview.cmd", "SetupVision.cmd", "CheckEnvironment.cmd"):
        shutil.copy2(ROOT / name, package / name)
    if args.prototype:
        shutil.copy2(ROOT / "StartPrototype1.cmd", package / "StartPrototype1.cmd")
        # The generic package entry also starts its local P1 exe; both read the same config.
    (package / "Tools").mkdir()
    shutil.copy2(ROOT / "Tools/SetupVision.ps1", package / "Tools/SetupVision.ps1")
    (package / "vision").mkdir()
    for name in ("main.py", "face_detector.py", "udp_sender.py", "settings.py", "setup_environment.py", "doctor.py",
                 "run_vision.cmd", "requirements.txt", "requirements-lock.txt"):
        shutil.copy2(ROOT / "vision" / name, package / "vision" / name)
    shutil.copy2(ROOT / ("docs/prototype1-operation.md" if args.prototype else "docs/distribution.md"), package / "README.md")
    shutil.copy2(ROOT / "docs/third-party.md", package / "THIRD-PARTY.md")
    manifest = dict(version=args.version, prototype=args.prototype, sourceCommit=revision, unity=summary["unityVersion"],
                    python="3.11.9 x64 (installed separately)", developmentBuild=False,
                    files={path.relative_to(package).as_posix(): digest(path)
                           for path in sorted(package.rglob("*")) if path.is_file()})
    (package / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as output:
        for path in sorted(package.rglob("*")):
            if path.is_file():
                output.write(path, path.relative_to(package.parent))
    checksum = digest(archive)
    archive.with_suffix(".zip.sha256").write_text(checksum + "  " + archive.name + "\n", encoding="ascii")
    print(json.dumps(dict(archive=archive.relative_to(ROOT).as_posix(), sha256=checksum,
                         files=len(manifest["files"]) + 1, bytes=archive.stat().st_size), indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
