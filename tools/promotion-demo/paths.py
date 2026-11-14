import os
import shutil
from pathlib import Path

SOURCE = Path(__file__).resolve().parent
REPO = SOURCE.parents[1]
KIT = REPO / "demonstratio-kit"
RUN = REPO / "artifacts" / "promotion-demo"
DB = RUN / "data" / "promotion-demo.db"
CAPTURE = RUN / "capture"
OUTPUT = RUN / "video"


def ffmpeg():
    executable = shutil.which(os.environ.get("FFMPEG", "ffmpeg"))
    if executable is None:
        raise SystemExit("Add FFmpeg to PATH or set FFMPEG to the path of ffmpeg.exe.")
    return executable
