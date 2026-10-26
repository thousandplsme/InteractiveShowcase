import json
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from settings import load_settings


class SettingsTests(unittest.TestCase):
    def test_checked_in_settings(self):
        settings = load_settings()
        self.assertEqual(settings["port"], 5005)
        self.assertEqual(settings["cameraIndex"], 0)
        self.assertTrue(settings["mirrorX"])
        self.assertTrue(settings["flipY"])

    def test_invalid_settings_fail_instead_of_silent_defaults(self):
        good = load_settings()
        variants = [{}, [], dict(good, port=True), dict(good, port=0), dict(good, port=5005.0),
                    dict(good, cameraIndex=-1), dict(good, mirrorX="false"),
                    dict(good, timeoutSeconds=float("nan")), dict(good, smoothTime=0),
                    dict(good, host="example.com")]
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "settings.json"
            for value in variants:
                with self.subTest(value=value):
                    path.write_text(json.dumps(value), encoding="utf-8")
                    with self.assertRaises(ValueError):
                        load_settings(path)
            path.write_text(json.dumps(good)[:-1] + ',"port":5006}', encoding="utf-8")
            with self.assertRaises(ValueError):
                load_settings(path)

    def test_bom_and_custom_settings(self):
        settings = dict(load_settings(), port=5006, mirrorX=False, smoothTime=.2)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "settings.json"
            path.write_text(json.dumps(settings), encoding="utf-8-sig")
            self.assertEqual(load_settings(path), settings)
