# Product screenshots

Use the seven numbered PNGs below for product listings and promotional galleries, in this order. Use the corresponding English captions alongside the images. The README, capture sources, and preview page are supporting files.

Each PNG is 1920 × 1080 (16:9). Screenshots show the application UI with built-in fictional sample tasks, rendered at 120% scale for legibility. Screenshot 7 adds a fictional investigation task to demonstrate Markdown source and live preview. There is no desktop, taskbar, browser chrome, added slogan, or decorative frame. The lead screenshot is also saved at the repository's canonical workspace image path, `docs/images/okf-todo-task-workspace.png`.

| Order | Screenshot | Caption (under 200 characters) |
| --- | --- | --- |
| 1 | [01-task-workspace.png](01-task-workspace.png) | Keep development and support work in one local workspace, with task notes, priorities, deadlines and tags. No online account required. |
| 2 | [02-focus-on-what-matters.png](02-focus-on-what-matters.png) | Focus on what needs action. Use Act now to find urgent and overdue work that is not waiting on someone else. |
| 3 | [03-track-follow-ups.png](03-track-follow-ups.png) | Keep follow-ups visible. See what is waiting, who or what you are waiting for, and the context behind each task. |
| 4 | [04-keep-the-details-together.png](04-keep-the-details-together.png) | Keep the evidence with the task. Track checklist progress, attach files and follow the history in the Timeline. |
| 5 | [05-work-in-dark-mode.png](05-work-in-dark-mode.png) | Choose a workspace that suits you. Switch between light and dark themes while keeping your task context close at hand. |
| 6 | [06-protect-your-local-work.png](06-protect-your-local-work.png) | Keep control of your local data. Create database backups and restore from a file, with a safety backup before restoration. |
| 7 | [07-write-with-markdown.png](07-write-with-markdown.png) | Write task notes in Markdown with a live preview. Keep headings, lists, code snippets and investigation notes together. |

## Presentation choices

1920 × 1080 balances legibility and file size. The UI is captured at 1600 × 900 logical pixels with a 1.2 device scale; the final image is rendered directly at 1920 × 1080, not enlarged from a smaller PNG. The detail view scrolls the supporting evidence into the upper part of the frame. Use the captions for selling points rather than embedding marketing text. These assets show the application itself, with each image highlighting a distinct benefit.

Use [preview.html](preview.html) to review the gallery and captions. The preview is not an upload asset.

## Regenerate

Requires the .NET SDK, restored solution dependencies and the Edge browser. From the repository root in PowerShell:

```powershell
dotnet restore .\Okf-Todo.UiTests\Okf-Todo.UiTests.csproj
.\docs\images\screenshots\capture.ps1
```

The opt-in capture uses the existing UI-test fixture, real frontend, bridge, services, migrations and sample-data seeder. It creates and removes a temporary SQLite database and isolated preferences. It never opens the user's task database. The capture code is compiled only with `ScreenshotCapture=true`; normal test builds do not include it. No application behavior is changed. MCP applicability: none, because this change only creates listing artwork and an opt-in capture utility.

Review regenerated images before publishing, especially after UI changes. Sample deadlines follow the capture date. The app version comes from the checkout being captured; generating these assets does not publish a listing or change the application package.
