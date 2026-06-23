import os
import tempfile
import unittest
from pathlib import Path
from unittest import mock


class EnvFileLoadingTests(unittest.TestCase):
    def test_load_env_file_populates_missing_values_without_overriding_existing_env(self):
        from LXMES.settings import _load_env_file

        with tempfile.TemporaryDirectory() as tmpdir:
            env_path = Path(tmpdir) / ".env"
            env_path.write_text(
                "\n".join(
                    [
                        "DB_HOST=192.168.10.86",
                        "DB_PORT=9959",
                        "DB_USER='lxmes'",
                        "EXISTING=from-file",
                        "# ignored comment",
                        "",
                    ]
                ),
                encoding="utf-8",
            )

            with mock.patch.dict(os.environ, {"EXISTING": "from-env"}, clear=False):
                for key in ("DB_HOST", "DB_PORT", "DB_USER"):
                    os.environ.pop(key, None)

                _load_env_file(env_path)

                self.assertEqual(os.environ["DB_HOST"], "192.168.10.86")
                self.assertEqual(os.environ["DB_PORT"], "9959")
                self.assertEqual(os.environ["DB_USER"], "lxmes")
                self.assertEqual(os.environ["EXISTING"], "from-env")
