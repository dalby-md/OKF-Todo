# Interactive repository check

Use this checklist after the September 2026 fixes. Record **Pass**, **Fail**, or **Not checked** for each numbered check. On failure, record the step, actual result, window size, theme, and a screenshot if useful. Work through one section at a time; send the results back for diagnosis before continuing if something fails.

Automated validation on 20 September 2026: Release solution build passed with zero warnings/errors; all 105 service/MCP tests passed; 31 browser tests passed in the final full run and the remaining ownership-preferences test passed after correcting its reload readiness wait. All 32 browser checks therefore passed across those runs. JavaScript syntax, Help section anchors, changed-file whitespace checks, and byte-for-byte normal Release Help synchronization passed. The actual executable also initialized a disposable database and generated all 50 sample tasks. Native Photino interaction and installed-package tests remain for the checks below.

## 1. Prepare a disposable workspace

Close any running OKF Todo instance; the desktop app allows only one instance. Paste this entire block into PowerShell. It uses absolute paths, so it works from any current directory and does not require an administrator shell:

```powershell
$checkProject = 'C:\git\Okf-Todo\Okf-Todo\Okf-Todo.csproj'
dotnet build $checkProject -c Release
if ($LASTEXITCODE -ne 0) { throw 'Build failed; stop here.' }

$checkDirectory = Join-Path 'C:\git\Okf-Todo\artifacts' ('manual-check-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $checkDirectory -ErrorAction Stop | Out-Null
$checkDatabase = Join-Path $checkDirectory 'okf-todo-check.db'
dotnet run --project $checkProject -c Release --no-build -- --database-path $checkDatabase
```

Keep this PowerShell window open. To restart against the same test data, rerun only the final `dotnet run` command; rerunning the whole block creates a new test workspace.

1. Confirm the first-run prompt appears; choose **Explore with sample data**. Expect 50 sample tasks, with no first-run prompt after loading.
2. Open **Settings → Data & maintenance** and verify the active database is the new `artifacts\manual-check-...` file before testing any deletion or maintenance action.

Task data is isolated by this launch. Appearance/editor preferences are still shared with your Windows account: note the original settings and restore them after testing. Use a separate Windows test account if you need complete preference isolation. Do not select your real database as a restore or backup destination.

## 2. Reported repairs

| Check | Actions | Expected result |
| --- | --- | --- |
| 3. Dialog focus (#2) | Settings → Data & values → Reset all colors. Press Shift+Tab, Tab repeatedly, then Escape. | Focus stays inside the confirmation. Escape leaves colors unchanged, keeps Preferences open, and returns focus to Reset all colors. A second Escape closes Preferences. |
| 4. Nested dialogs (#2) | Open a task type editor from Data & values; Escape back through editor, list, and Preferences. Also cancel an unsaved-task prompt. | Only the top dialog closes each time; focus returns to its opener. No typing or shortcuts reach the workspace behind it. |
| 5. Tags (#3) | At 1280×720, select **Follow up on ServiceDesk incident**. Add several tags and one long unbroken tag. Save, remove a tag, save again. Repeat at 1600×900 and Largest text. | Chips wrap, removal works, saved tags survive reselection; task metadata introduces no horizontal scrolling. |
| 6. Labels (#4) | Open New task. Create **CHECK — support handover**. Add two checklist items and complete one. | Primary action says Create task; it saves immediately. Main button remains Save; lifecycle button says Cancel task. Checklist pill has a symbol and tooltip Checklist: 1 of 2 completed. Dialog Cancel only dismisses. |
| 7. Timeline (#6) | Add two comments using Add comment beside the comment box and change priority. Switch All activity / Comments / Changes. Tab to Go to comment box and activate it. | Correct entries appear in chronological order, no duplicate Comment added row, composer receives focus. Timeline stays last. Reopening the task resets the filter. |
| 8. Views (#7) | Mark the CHECK task Urgent and Waiting for customer logs. Compare Active, Ready, Waiting, Attention, Act now. Clear waiting and save. | Each view has a visible accurate description. Waiting urgent work appears in Active, Waiting, Attention; clearing waiting makes it eligible for Ready and Act now. Completed/Cancelled remain explicit in All. |
| 9. Hidden context (#8) | Set Owner, Responsible, Source, and a relationship; save. Disable their visibility preferences. Select the task and Show additional details. Navigate its relationship. | Notice identifies existing hidden data. Fields and relationships can be inspected; empty optional fields stay unobtrusive. Preferences stay off. Selecting another task resets temporary visibility. |
| 10. Export (#9) | Filter to CHECK tasks. Export Brief list to Markdown, then Support handover with Copy as HTML; paste into a rich-text destination. Compare Full inventory and Customize. Repeat in All lists. | Scope, count, and included fields are visible. Output matches the filtered tasks and chosen columns. List appears only for All lists. No bodies, comments, or attachments are exported. Custom order/sorting persists; Saved recipe restores the dialog's opening recipe. |
| 11. Empty Trash (#10) | Open empty Trash. Then move the CHECK task to Trash, inspect it, restore it, and repeat with Undo. Try a search that matches nothing while Trash is populated. | Empty Trash explains recovery and shows no disabled editor. Populated Trash supports read-only inspection and restoration. A filtered empty result says No matching tasks. |
| 12. Help (#11) | Open Help → desktop guide. Use the topic index with mouse and Tab/Enter, especially Timeline and comments, export, and backup. | Every link scrolls to and focuses its section heading. Getting started remains near the top. Repeat in dark theme. |

Issue #5 is a [separate follow-up scheduling design](../waiting-follow-up-design.md), not an implemented feature. Review its date/lifecycle/MCP decisions separately; no Follow up on field is expected in this build.

After check 6, also test scrolling at the window size where the problem occurred: add a comment, point over Checklist or Timeline, and scroll to the bottom with the mouse wheel. Repeat by dragging the task-details scrollbar at the far right. Expect to reach the saved comment and the entire comment box with space below it; the task queue should stay in place. Repeat with **Largest** text and the **Stacked** layout. Keep using the same disposable database when rebuilding/restarting so the saved CHECK task and comment are available for this retest.

Scrolling follow-up validation: five viewport/font combinations passed mouse-wheel and scrollbar-drag checks, including HTML and Markdown editors and light/dark scrollbar visibility. All 14 affected browser checks passed together (scrolling, existing audit repairs, task creation, responsive workspace, and read-only detail layout). The reported native desktop window still needs the retest above.

Native window placement retest: restart with the same test database and confirm the complete window fits above the Windows taskbar, with the task-details scrollbar visible at the right edge. Drag that scrollbar to reach Timeline and the comment box. Close and reopen again to check placement persistence. If available, also repeat after disconnecting a secondary monitor or changing display scaling/resolution. Browser tests alone do not validate native window placement.

Native placement validation on 20 September 2026: the real Photino window on the 150% scaled display was corrected from 1946×1226 to the monitor's 1920×1128 work area. The isolated window closed automatically without opening a database. Seven placement unit cases and the existing maximized-preference persistence test passed. The full app's visible scrollbar still needs the manual retest above.

## 3. Core workflow regression

13. Create one task in HTML mode and one in Markdown. Edit title and body, then change task/view: test **Cancel**, **Save**, and **Discard** separately. Expect cancellation to preserve edits, Save to persist them, and only Discard to lose them.
14. Complete, reopen, and cancel a CHECK task. Verify views, selection, status labels, and read-only behavior. Enable completed/cancelled editing independently and verify each affects only its corresponding state.
15. Add a small text attachment; download it and compare content. Add/edit/reorder/complete checklist items, add/remove a relationship, and add/delete a test comment. Verify task context and Timeline after each action.
16. Create a second list, move the CHECK task, bulk-move and Undo, then delete the second list with a destination. Expect all tasks preserved and each move recorded in Timeline.
17. Star active and finished tasks; inspect the Starred Finished group. Select a filtered subset, move it to Trash, Undo, restore, and permanently delete only a disposable CHECK task after reviewing confirmation.
18. On the disposable database, create a backup to a new file in the check directory. Test cancelling the picker. Add a CHECK task, restore the backup, close/restart using the same `$checkDatabase`, and verify the backed-up state and safety backup. Never use a personal database file here.
19. Remove sample data and confirm personal CHECK tasks survive, including one tagged `sample-data`. If testing reset, confirm the check database path again, type RESET DATABASE, restart, and inspect the empty state and safety backup.
20. Close/reopen the app with the same command. Confirm saved task data, lists, tags, and chosen preferences persist. Repeat key keyboard and layout checks in light/dark themes, at 1280×720 and 1600×900, and with enlarged text. Restore your original appearance preferences when done.

## 4. Automated confirmation and boundaries

```powershell
Set-Location -LiteralPath 'C:\git\Okf-Todo' -ErrorAction Stop
dotnet test .\Okf-Todo.Tests\Okf-Todo.Tests.csproj -c Release
dotnet test .\Okf-Todo.UiTests\Okf-Todo.UiTests.csproj -c Release
node --check .\Okf-Todo\wwwroot\js\app.js
cmd /c fc /b docs\help\using-okf-todo.md Okf-Todo\bin\Release\net8.0\wwwroot\help\using-okf-todo.md
```

The service suite includes real MCP stdio checks; browser tests use actual application services with temporary databases. They do not establish native WebView, operating-system clipboard, file-picker, or installer correctness. The steps above cover the native app checks. Before a release, also rebuild and run the documented [installed contract tests](installed-contract-tests.md) for the exact package being shipped.

Completion means every applicable check is Pass, automated checks pass, and any Not checked item is explicitly accepted as a remaining validation gap. It does not prove the absence of every possible bug.
