# Microsoft Store screenshots

Upload the six numbered PNGs below to the **Desktop screenshots** section of the English Microsoft Store listing, in this order. Paste the corresponding caption into Partner Center. Do not upload this README, the capture sources, or the preview page.

Each PNG is 1920 × 1080 (16:9). Screenshots show the current application UI with built-in fictional sample tasks, rendered at 120% scale for legibility. There is no desktop, taskbar, browser chrome, added slogan, or decorative frame. The lead screenshot is also saved at the repository's canonical Store image path, `docs/images/okf-todo-task-workspace.png`.

| Order | Screenshot | Store caption (under 200 characters) |
| --- | --- | --- |
| 1 | [01-task-workspace.png](01-task-workspace.png) | Keep development and support work in one local workspace, with task notes, priorities, deadlines and tags. No online account required. |
| 2 | [02-focus-on-what-matters.png](02-focus-on-what-matters.png) | Focus on what needs action. Use Act now to find urgent and overdue work that is not waiting on someone else. |
| 3 | [03-track-follow-ups.png](03-track-follow-ups.png) | Keep follow-ups visible. See what is waiting, who or what you are waiting for, and the context behind each task. |
| 4 | [04-keep-the-details-together.png](04-keep-the-details-together.png) | Keep the evidence with the task. Track checklist progress, attach files and follow the history in the Timeline. |
| 5 | [05-work-in-dark-mode.png](05-work-in-dark-mode.png) | Choose a workspace that suits you. Switch between light and dark themes while keeping your task context close at hand. |
| 6 | [06-protect-your-local-work.png](06-protect-your-local-work.png) | Keep control of your local data. Create database backups and restore from a file, with a safety backup before restoration. |

## Store-specific choices

Microsoft's [MSIX screenshot guidance](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/screenshots-and-images) accepts Desktop PNGs at 1366 × 768 or larger, including 4K, with a maximum of 50 MB per image. It recommends at least four screenshots, no added logos or marketing messages, captions of up to 200 characters, and important content in the top two-thirds. Checked 18 September 2026.

1920 × 1080 is a deliberate legibility/size choice, not a Microsoft-mandated optimum. The UI is captured at 1600 × 900 logical pixels with a 1.2 device scale; the final image is rendered directly at 1920 × 1080, not enlarged from a smaller PNG. The detail view scrolls the supporting evidence into the upper part of the frame. Use the captions for selling points rather than embedding marketing text. These are screenshot-slot assets, not the separate Store icon or Super hero art.

Use [preview.html](preview.html) to review the gallery and captions. The preview is not an upload asset.

## Regenerate

Requires the .NET SDK, restored solution dependencies and Microsoft Edge. From the repository root in PowerShell:

```powershell
dotnet restore .\Okf-Todo.UiTests\Okf-Todo.UiTests.csproj
.\docs\images\microsoft-store\capture.ps1
```

The opt-in capture uses the existing UI-test fixture, real frontend, bridge, services, migrations and sample-data seeder. It creates and removes a temporary SQLite database and isolated preferences. It never opens the user's task database. The capture code is compiled only with `StoreScreenshotCapture=true`; normal test builds do not include it. No application behavior is changed. MCP applicability: none, because this change only creates listing artwork and an opt-in capture utility.

Review regenerated images before uploading, especially after UI changes. Sample deadlines follow the capture date. The app version comes from the checkout being captured; these assets do not publish a Store submission or change its package.
