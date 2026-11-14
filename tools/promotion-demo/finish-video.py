import subprocess

from assemble import encode
from paths import CAPTURE, OUTPUT, ffmpeg


def main():
    encoder = ffmpeg()
    video = OUTPUT / "excel-to-tasks.mp4"
    encode(encoder, [
        "-f", "concat", "-safe", "1", "-i", str(CAPTURE / "concat.txt"), "-an",
        "-c:v", "libx264", "-preset", "medium", "-crf", "19", "-pix_fmt", "yuv420p",
        "-movflags", "+faststart", str(video),
    ], "mp4-encode.log")
    subprocess.run([encoder, "-v", "error", "-xerror", "-i", str(video), "-f", "null", "-"], check=True)
    print(f"Decoded successfully: {video}")


if __name__ == "__main__":
    main()
