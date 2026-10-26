"""Shared startup settings. Network destination always remains localhost."""
import json
import math
from pathlib import Path

DEFAULT_PATH = Path(__file__).resolve().parents[1] / "config" / "vision.json"


def unique_fields(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate settings field: " + key)
        result[key] = value
    return result


def load_settings(path=DEFAULT_PATH):
    with Path(path).open(encoding="utf-8-sig") as stream:
        value = json.load(stream, object_pairs_hook=unique_fields)
    if not isinstance(value, dict) or set(value) != {
        "cameraIndex", "port", "mirrorX", "flipY", "smoothTime", "timeoutSeconds"
    }:
        raise ValueError("Settings must contain exactly the six documented fields")
    for key, low, high in (("cameraIndex", 0, 99), ("port", 1, 65535)):
        if type(value[key]) is not int or not low <= value[key] <= high:
            raise ValueError(f"Invalid {key}: expected integer {low}..{high}")
    for key in ("mirrorX", "flipY"):
        if type(value[key]) is not bool:
            raise ValueError(f"Invalid {key}: expected true or false")
    for key, low, high in (("smoothTime", .001, 5), ("timeoutSeconds", .1, 10)):
        if type(value[key]) not in (int, float) or not math.isfinite(value[key]) or not low <= value[key] <= high:
            raise ValueError(f"Invalid {key}: expected finite number {low}..{high}")
    return value
