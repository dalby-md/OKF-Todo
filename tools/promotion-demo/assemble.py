import json
import shutil
import subprocess

from paths import CAPTURE, OUTPUT, ffmpeg


def encode(encoder, args, log_name):
    result = subprocess.run(
        [encoder, "-hide_banner", "-y", *args], capture_output=True, text=True, encoding="utf-8",
        errors="replace",
    )
    (CAPTURE / log_name).write_text(result.stderr, encoding="utf-8")
    result.check_returncode()


def main():
    encoder = ffmpeg()
    clips = json.loads((CAPTURE / "clips.json").read_text(encoding="utf-8"))
    chapters = []
    files = []
    offset = 0
    for clip in clips:
        name = clip["name"]
        destination = CAPTURE / f"{name}-trim.webm"
        encode(encoder, [
            "-ss", str(clip["start"]), "-i", str(CAPTURE / clip["path"]),
            "-t", str(clip["duration"]), "-an", "-c:v", "libvpx",
            "-b:v", "1800k", "-crf", "10", "-deadline", "good", "-cpu-used", "4",
            str(destination),
        ], f"{name}-encode.log")
        files.append(f"file '{destination.name}'")
        duration = clip["duration"]
        chapters.append(dict(name=name, start=round(offset, 2), duration=duration))
        offset += duration

    concat = CAPTURE / "concat.txt"
    concat.write_text("\n".join(files) + "\n", encoding="utf-8")
    OUTPUT.mkdir(parents=True, exist_ok=True)
    encode(encoder, ["-f", "concat", "-safe", "1", "-i", str(concat), "-c", "copy",
                     str(OUTPUT / "excel-to-tasks.webm")], "assemble.log")
    shutil.copyfile(CAPTURE / "01-source.png", OUTPUT / "poster.png")
    (OUTPUT / "chapters.json").write_text(json.dumps(chapters, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
