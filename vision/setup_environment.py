"""Install locked dependencies into a dedicated environment; never touch global packages."""
import argparse
from pathlib import Path
import struct
import subprocess
import sys
import venv


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--environment", type=Path, default=Path(__file__).resolve().parent / ".venv")
    args = parser.parse_args()
    if sys.version_info[:2] != (3, 11) or struct.calcsize("P") != 8 or sys.platform != "win32":
        parser.error("Use Windows Python 3.11 x64; no global Python installation is changed")
    environment = args.environment.resolve()
    python = environment / "Scripts" / "python.exe"
    try:
        if environment.exists():
            if not (environment / "pyvenv.cfg").is_file() or not python.is_file():
                raise RuntimeError("Target exists but is not a usable venv; refusing to overwrite it")
        else:
            venv.create(environment, with_pip=True)
        subprocess.run([str(python), "-c",
                        "import sys,struct; assert sys.version_info[:2]==(3,11) and struct.calcsize('P')==8"], check=True)
        subprocess.run([str(python), "-m", "pip", "install", "--disable-pip-version-check", "-r",
                        str(Path(__file__).with_name("requirements-lock.txt"))], check=True)
        subprocess.run([str(python), "-m", "pip", "check"], check=True)
    except (OSError, RuntimeError, subprocess.CalledProcessError) as error:
        print("Setup failed: " + str(error), file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
