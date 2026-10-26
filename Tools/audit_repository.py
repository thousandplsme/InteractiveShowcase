"""Audit tracked/candidate sources, Unity meta files, and ignore rules without changing Git."""
from pathlib import Path, PurePosixPath
import re
import subprocess

ROOT = Path(__file__).resolve().parents[1]


def main():
    listed = subprocess.check_output(["git", "ls-files", "-co", "--exclude-standard", "-z"], cwd=ROOT)
    names = sorted(set(listed.decode("utf-8").strip("\0").split("\0")))
    errors = []
    largest = (0, "")
    secrets = re.compile(r"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----|gh[pousr]_[A-Za-z0-9]{30,}|AKIA[A-Z0-9]{16}")
    forbidden = {"library", "temp", "logs", "obj", "build", "builds", "usersettings", ".venv", "__pycache__", "testresults", "dist", "captures", "recordings", "face-data"}
    candidates = set(names)
    for name in names:
        path = ROOT / name
        if not path.is_file():
            continue
        parts = PurePosixPath(name).parts
        if any(part.lower() in forbidden for part in parts) or path.name == ".env" or path.suffix in (".pem", ".key"):
            errors.append("Forbidden candidate: " + name)
        size = path.stat().st_size
        largest = max(largest, (size, name))
        if size > 5 * 1024 * 1024:
            errors.append("Unexpected large source: " + name)
        if path.suffix in (".cs", ".py", ".md", ".txt", ".json", ".asset", ".unity", ".ps1", ".cmd"):
            if secrets.search(path.read_text(encoding="utf-8-sig", errors="replace")):
                errors.append("Possible secret pattern: " + name)  # Never print the matched value.
        if name.startswith("Assets/") and not name.endswith(".meta"):
            if name + ".meta" not in candidates:
                errors.append("Missing/untracked meta: " + name)
            for parent in PurePosixPath(name).parents:
                if str(parent) in ("Assets", "."):
                    break
                if str(parent) + ".meta" not in candidates:
                    errors.append("Missing/untracked folder meta: " + str(parent))
    ignored = ["Library/a", "unity/InteractiveShowcase/Library/a", "Builds/a.exe", "vision/.venv/a",
               "vision/__pycache__/a.pyc", ".env", "private.key", "captures/a.png", "TestResults/a.json", "dist/a.zip"]
    result = subprocess.run(["git", "check-ignore", "-z", "--stdin"], cwd=ROOT,
                            input=("\0".join(ignored) + "\0").encode("utf-8"), capture_output=True, check=False)
    if result.returncode not in (0, 1):
        errors.append("git check-ignore failed; ignore rules were not verified")
    actual = set(result.stdout.decode("utf-8").split("\0"))
    errors.extend("Ignore rule missing: " + name for name in ignored if name not in actual)
    for error in sorted(set(errors)):
        print(error)
    print(f"Candidates: {len(names)}; largest: {largest[0]} bytes ({largest[1]}); errors: {len(set(errors))}")
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
