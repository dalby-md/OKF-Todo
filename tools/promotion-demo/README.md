# Reproduce the Excel-to-tasks promotion video

The [demo kit](../../demonstratio-kit/README.md) includes the fictional workbook and approved plan. The scripts replay that plan through OKF Todo's command interface and record its frontend in a local browser host. They use a new demo database under `artifacts/promotion-demo/data`. The published [MP4](../../demonstratio-kit/video/excel-to-tasks.mp4) and [transcript](../../demonstratio-kit/video/transcript.md) document the September 2026 recording. This run uses the current checkout and recreates the nine scenes. The scene wording was reconstructed from the transcript where the old local scene file had been overwritten.

On Windows, install a .NET SDK that can build the repository, the .NET 8 and ASP.NET Core 8 runtimes, Python 3, Node.js 20 or later with npm, and an FFmpeg build with `libvpx` and `libx264`. Make `python` available in the shell and add FFmpeg to PATH, or set `$env:FFMPEG` to the full path of `ffmpeg.exe`. The first setup needs access to NuGet, npm, and the Playwright browser download.

From the checkout root, run these PowerShell commands.

```powershell
dotnet build tools/promotion-demo/host/DemoHost.csproj -c Release
npm.cmd --prefix tools/promotion-demo ci
Push-Location tools/promotion-demo
npx.cmd playwright install chromium
Pop-Location

python tools/promotion-demo/commands.py discover
python tools/promotion-demo/save-plan.py
python tools/promotion-demo/build-scenes.py
```

Start the loopback browser host in a second terminal at the checkout root. Leave it running during capture.

```powershell
dotnet tools/promotion-demo/host/bin/Release/net8.0/DemoHost.dll
```

Run the capture and encoders from the checkout root in the first terminal.

```powershell
npm.cmd --prefix tools/promotion-demo run record
python tools/promotion-demo/assemble.py
python tools/promotion-demo/finish-video.py
```

The finished MP4, WebM, poster and chapter list appear under `artifacts/promotion-demo/video`. The MP4 script decodes the entire file to check for errors. Restart the host if its URL file points to a stopped process. Stop the host and rename `artifacts/promotion-demo` before starting a new replay from the workbook. This preserves previous output while the next run creates its own database.

To reproduce the rehearsal as well, open the loopback URL in `artifacts/promotion-demo/browser-url.txt`. Skip sample data, open the investigation, and add the Timeline comment `Demo review: source notes checked against the saved plan.` through the UI. Then run:

```powershell
python tools/promotion-demo/verify-result.py
```

The verifier checks both task bodies and their fields, all six unchecked checklist items, the wait for Maria, and the single review comment. It saves its read-back as `artifacts/promotion-demo/rehearsal-result.json`. For the browser playback check, stop the host when finished recording, run `python tools/promotion-demo/serve-player.py`, and open the printed loopback URL. Stop the player with Ctrl+C.
