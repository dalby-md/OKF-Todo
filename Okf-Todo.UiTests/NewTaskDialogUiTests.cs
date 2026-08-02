using System.Net;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Photino.Okf_Todo.Bridge;
using Photino.Okf_Todo.Data;
using Photino.Okf_Todo.Services;

namespace Okf_Todo.UiTests;

public sealed class NewTaskDialogUiTests
{
    private const string BridgeAdapterScript = """
        (() => {
          const listeners = [];
          window.chrome = window.chrome || {};
          window.chrome.webview = {
            addEventListener(type, listener) {
              if (type === 'message') listeners.push(listener);
            },
            postMessage(message) {
              fetch('/__ui-test/bridge', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: message
              })
                .then(response => response.text())
                .then(data => listeners.forEach(listener => listener({ data })))
                .catch(error => console.error('UI test bridge failed.', error));
            }
          };
        })();
        """;

    private const string ClipboardAdapterScript = """
        (() => {
          window.__clipboardWrite = null
          class TestClipboardItem {
            constructor(items) {
              this.items = items
            }
          }
          Object.defineProperty(window, 'ClipboardItem', {
            configurable: true,
            value: TestClipboardItem
          })
          Object.defineProperty(navigator, 'clipboard', {
            configurable: true,
            value: {
              async write(items) {
                const item = items[0]
                window.__clipboardWrite = {
                  html: await item.items['text/html'].text(),
                  plainText: await item.items['text/plain'].text()
                }
              }
            }
          })
        })()
        """;

    [Fact]
    public async Task EmptyDatabase_OffersRemovableSampleDataAndPreservesFirstRunChoice()
    {
        await using var fixture = await UiAppFixture.CreateAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1600, Height = 1000 }
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);

        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{fixture.BaseUrl}/index.html?v=database-first-run",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        await page.Locator("#first-run-new-task-button").WaitForAsync();
        var firstRunOverlay = page.Locator("#first-run-overlay");
        var firstRunDialog = firstRunOverlay.GetByRole(AriaRole.Dialog);
        Assert.Equal("true", await firstRunDialog.GetAttributeAsync("aria-modal"));
        Assert.True(await page.Locator(".app-shell > :not(#first-run-overlay)").EvaluateAllAsync<bool>(
            "elements => elements.every(element => element.inert)"));
        await page.WaitForFunctionAsync("() => document.activeElement?.id === 'first-run-new-task-button'");
        var dialogBounds = await firstRunDialog.BoundingBoxAsync();
        Assert.NotNull(dialogBounds);
        Assert.InRange(Math.Abs((dialogBounds.X + (dialogBounds.Width / 2)) - 800), 0, 2);
        Assert.InRange(Math.Abs((dialogBounds.Y + (dialogBounds.Height / 2)) - 500), 0, 2);
        await page.GetByText(
            "Sample data is easy to remove anytime from Settings → Data & maintenance.",
            new PageGetByTextOptions { Exact = false }).WaitForAsync();
        await page.Locator("#first-run-sample-button").ClickAsync();
        await page.Locator("#first-run-sample-button[aria-busy='true'] .button-spinner").WaitForAsync();
        Assert.Equal(
            "Adding sample data...",
            await page.Locator("#first-run-sample-button .sample-data-action-label").TextContentAsync());
        Assert.True(await page.Locator("#first-run-new-task-button").IsDisabledAsync());
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-list .task-row').length === 30");
        Assert.True(await firstRunOverlay.IsHiddenAsync());
        Assert.True(await page.Locator(".app-shell > :not(#first-run-overlay)").EvaluateAllAsync<bool>(
            "elements => elements.every(element => !element.inert)"));

        Assert.Equal("Settings", await page.Locator("#settings-button").GetAttributeAsync("aria-label"));
        Assert.Equal("Settings", await page.Locator("#settings-button").GetAttributeAsync("title"));
        await page.Locator("#settings-button").GetByText("Settings", new LocatorGetByTextOptions
        {
            Exact = true
        }).WaitForAsync();
        await page.Locator("#settings-button").ClickAsync();
        Assert.Equal(
            "Data & maintenance",
            await page.Locator("[data-preference-section='database']").TextContentAsync());
        await page.Locator("[data-preference-section='database']").ClickAsync();
        Assert.Equal("Data & maintenance", await page.Locator("#preferences-panel-title").TextContentAsync());
        Assert.True(await page.Locator("#remove-sample-data-button").IsVisibleAsync());
        Assert.False(await page.Locator("#add-sample-data-button").IsVisibleAsync());

        await page.Locator("#reset-empty-database-button").ClickAsync();
        await page.Locator("#database-reset-confirmation").FillAsync("reset database");
        Assert.False(await page.Locator("#database-reset-confirm-button").IsDisabledAsync());
        await page.Locator("#database-reset-cancel-button").ClickAsync();

        await page.Locator("#remove-sample-data-button").ClickAsync();
        await page.GetByText(
            "Tasks you created yourself are not affected",
            new PageGetByTextOptions { Exact = false }).WaitForAsync();
        await page.Locator("#confirmation-confirm-button").ClickAsync();
        await page.Locator("#first-run-new-task-button").WaitForAsync();
        Assert.True(await page.Locator(".app-shell > :not(#first-run-overlay)").EvaluateAllAsync<bool>(
            "elements => elements.every(element => element.inert)"));

        await page.Locator("#first-run-new-task-button").ClickAsync();
        await page.Locator("#new-task-overlay").WaitForAsync();
        Assert.True(await firstRunOverlay.IsHiddenAsync());
        await page.Locator("#new-task-cancel-button").ClickAsync();
        await firstRunOverlay.WaitForAsync();

        Assert.Contains("database.sample.create", fixture.BridgeMessageTypes);
        Assert.Contains("database.sample.remove", fixture.BridgeMessageTypes);
    }

    [Fact]
    public async Task AppearanceFontSize_DefaultsToStandardAndPersistsAllSupportedScales()
    {
        await using var fixture = await UiAppFixture.CreateAsync();
        await fixture.SendBridgeAsync("task.create", new
        {
            title = "Font size preference test",
            taskTypeCode = "NOTE",
            body = "",
            bodyFormatCode = "HTML",
            taskPriorityCode = "NORMAL"
        });
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1487, Height = 1058 }
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);
        await context.AddInitScriptAsync("""
            (() => {
              window.__firstRunModalWasVisible = false
              new MutationObserver(() => {
                const overlay = document.querySelector('#first-run-overlay')
                if (overlay && !overlay.hidden) {
                  window.__firstRunModalWasVisible = true
                }
              }).observe(document, { childList: true, subtree: true, attributes: true, attributeFilter: ['hidden'] })
            })()
            """);

        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{fixture.BaseUrl}/index.html?v=appearance-font-size",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-type option').length > 0");
        Assert.False(await page.EvaluateAsync<bool>("() => window.__firstRunModalWasVisible"));
        var htmlEditorBody = page.FrameLocator("#editor-host .tox-edit-area iframe").Locator("body");
        await htmlEditorBody.WaitForAsync();

        Assert.Equal(
            "16px",
            await page.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).fontSize"));
        Assert.Equal("16px", await htmlEditorBody.EvaluateAsync<string>("body => getComputedStyle(body).fontSize"));

        await OpenAppearancePreferencesAsync(page);
        Assert.Equal(5, await page.Locator("[data-preference-select='font-size'] .preference-choice").CountAsync());
        Assert.True(await page.Locator("[data-preference-select='font-size'] .preference-choice[data-value='STANDARD']")
            .EvaluateAsync<bool>("button => button.classList.contains('is-selected')"));

        async Task SelectFontSizeAsync(string code, string pixels)
        {
            await page.Locator("#save-status").EvaluateAsync("element => { element.textContent = 'Waiting for font size save' }");
            await page.Locator($"[data-preference-select='font-size'] .preference-choice[data-value='{code}']").ClickAsync();
            await page.WaitForFunctionAsync(
                "pixels => getComputedStyle(document.documentElement).fontSize === pixels && document.querySelector('#save-status').textContent === 'Font size saved'",
                pixels);
        }

        await SelectFontSizeAsync("SMALLEST", "12px");
        Assert.Equal("12px", await htmlEditorBody.EvaluateAsync<string>("body => getComputedStyle(body).fontSize"));

        await SelectFontSizeAsync("SMALL", "14px");
        await SelectFontSizeAsync("LARGE", "18px");
        await SelectFontSizeAsync("LARGEST", "20px");
        Assert.Equal("20px", await htmlEditorBody.EvaluateAsync<string>("body => getComputedStyle(body).fontSize"));
        await page.Locator("#settings-close-button").ClickAsync();

        await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-type option').length > 0");
        Assert.Equal(
            "20px",
            await page.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).fontSize"));
        htmlEditorBody = page.FrameLocator("#editor-host .tox-edit-area iframe").Locator("body");
        await htmlEditorBody.WaitForAsync();
        Assert.Equal("20px", await htmlEditorBody.EvaluateAsync<string>("body => getComputedStyle(body).fontSize"));
        await OpenAppearancePreferencesAsync(page);
        Assert.True(await page.Locator("[data-preference-select='font-size'] .preference-choice[data-value='LARGEST']")
            .EvaluateAsync<bool>("button => button.classList.contains('is-selected')"));
        Assert.Contains("layout.preference.save", fixture.BridgeMessageTypes);
    }

    [Fact]
    public async Task AppearanceFontSize_AlsoControlsMarkdownEditorBaseText()
    {
        await using var fixture = await UiAppFixture.CreateAsync();
        await fixture.SendBridgeAsync("editor.preference.save", new
        {
            bodyFormatCode = "MARKDOWN",
            markdownEditType = "MARKDOWN"
        });
        await fixture.SendBridgeAsync("task.create", new
        {
            title = "Markdown editor font size test",
            taskTypeCode = "NOTE",
            body = "Plain Markdown body",
            bodyFormatCode = "MARKDOWN",
            taskPriorityCode = "NORMAL"
        });

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1487, Height = 1058 }
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);

        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{fixture.BaseUrl}/index.html?v=markdown-editor-font-size",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        var codeMirror = page.Locator("#editor-host .CodeMirror").First;
        await codeMirror.WaitForAsync();
        Assert.Equal("16px", await codeMirror.EvaluateAsync<string>("editor => getComputedStyle(editor).fontSize"));

        await OpenAppearancePreferencesAsync(page);
        await page.Locator("[data-preference-select='font-size'] .preference-choice[data-value='LARGEST']").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => getComputedStyle(document.querySelector('#editor-host .CodeMirror')).fontSize === '20px'");
        await page.Locator("#settings-close-button").ClickAsync();

        await page.Locator("#editor-host .te-switch-button.wysiwyg").ClickAsync();
        var wysiwygBody = page.Locator("#editor-host .te-ww-container .tui-editor-contents");
        await wysiwygBody.WaitForAsync();
        Assert.Equal("20px", await wysiwygBody.EvaluateAsync<string>("editor => getComputedStyle(editor).fontSize"));
    }

    [Theory]
    [InlineData("HTML")]
    [InlineData("MARKDOWN")]
    public async Task FunctionKeys_WorkFromMainPageAndBodyEditor(string bodyFormatCode)
    {
        await using var fixture = await UiAppFixture.CreateAsync();
        await fixture.SendBridgeAsync("editor.preference.save", new
        {
            bodyFormatCode,
            markdownEditType = "MARKDOWN",
            editorHeight = 360
        });
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1600, Height = 1000 }
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);

        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{fixture.BaseUrl}/index.html?v=function-key-shortcuts-{bodyFormatCode.ToLowerInvariant()}",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-type option').length > 0");

        Assert.Equal("F1", await page.Locator("#help-button").GetAttributeAsync("aria-keyshortcuts"));
        Assert.Equal("F2", await page.Locator("#new-task-button").GetAttributeAsync("aria-keyshortcuts"));
        Assert.Equal("F3", await page.Locator("#task-search").GetAttributeAsync("aria-keyshortcuts"));
        Assert.Equal("F8", await page.Locator("#save-button").GetAttributeAsync("aria-keyshortcuts"));
        Assert.Equal("Save task (F8)", await page.Locator("#save-button").GetAttributeAsync("aria-label"));
        Assert.Equal("Save task (F8)", await page.Locator("#save-shortcut-tooltip").GetAttributeAsync("title"));
        Assert.Equal("F9", await page.Locator("#complete-button").GetAttributeAsync("aria-keyshortcuts"));
        Assert.Equal("F3", await page.Locator(".task-search-field kbd").TextContentAsync());
        Assert.Equal(
            6,
            await page.Locator("[data-save-shortcut][aria-keyshortcuts='F8']").CountAsync());

        await page.Keyboard.PressAsync("F2");
        await page.Locator("#new-task-overlay").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        var taskTitle = $"Function keys from {bodyFormatCode}";
        await page.Locator("#new-task-title-input").FillAsync(taskTitle);
        await page.Keyboard.PressAsync("F8");
        await page.Locator("#new-task-overlay").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden
        });
        await page.EvaluateAsync("() => window.Editor.focus()");
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#editor-host')?.contains(document.activeElement)");
        await page.Keyboard.PressAsync("F3");
        await page.WaitForFunctionAsync("() => document.activeElement?.id === 'task-search'");

        await page.EvaluateAsync("() => window.Editor.focus()");
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#editor-host')?.contains(document.activeElement)");
        await page.Keyboard.PressAsync("F1");
        await page.Locator("#help-overlay").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        await page.Locator("#help-close-button").ClickAsync();

        await page.EvaluateAsync("() => window.Editor.focus()");
        await page.Keyboard.PressAsync("F2");
        await page.Locator("#new-task-overlay").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        await page.Locator("#new-task-cancel-button").ClickAsync();

        var updatedTitle = $"{taskTitle} saved by F8";
        await page.Locator("#task-title").FillAsync(updatedTitle);
        await page.EvaluateAsync("() => window.Editor.focus()");
        await page.Keyboard.PressAsync("F8");
        await page.WaitForFunctionAsync("() => document.querySelector('#save-status')?.textContent === 'Saved'");
        Assert.Contains("task.update", fixture.BridgeMessageTypes);

        await page.EvaluateAsync("() => window.Editor.focus()");
        await page.Keyboard.PressAsync("F9");
        await page.WaitForFunctionAsync(
            """
            () => document.querySelector('#task-read-only-title')?.textContent === 'Completed task — read only'
              && document.querySelector('#task-read-only-notice')?.hidden === false
              && document.querySelector('#complete-button')?.textContent === 'Reopen'
            """);
        Assert.Contains("task.complete", fixture.BridgeMessageTypes);
    }

    [Theory]
    [InlineData("HTML")]
    [InlineData("MARKDOWN")]
    public async Task SaveNewTask_WithoutMainSave_PersistsTaskEnablesControlsAndFocusesEditor(
        string bodyFormatCode)
    {
        await using var fixture = await UiAppFixture.CreateAsync();
        await fixture.SendBridgeAsync("editor.preference.save", new
        {
            bodyFormatCode,
            markdownEditType = "MARKDOWN",
            editorHeight = 360
        });
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1600, Height = 1000 }
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);

        var page = await context.NewPageAsync();
        string? appScriptUrl = null;
        page.Request += (_, request) =>
        {
            if (new Uri(request.Url).AbsolutePath == "/js/app.js")
            {
                appScriptUrl = request.Url;
            }
        };
        const string startupVersion = "new-task-save-contract";
        await page.GotoAsync($"{fixture.BaseUrl}/index.html?v={startupVersion}", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-type option').length > 0");
        Assert.Equal(
            $"/css/app.css?v={startupVersion}",
            await page.Locator("#app-stylesheet").GetAttributeAsync("href"));
        Assert.Contains($"v={startupVersion}", appScriptUrl);

        var taskTitle = $"New task dialog {bodyFormatCode} browser contract";
        await page.Locator("#new-task-button").ClickAsync();
        await page.Locator("#new-task-title-input").FillAsync(taskTitle);
        await page.Locator("#new-task-save-button").ClickAsync();

        await page.Locator("#new-task-overlay").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden
        });

        Assert.Equal(taskTitle, await page.Locator("#task-title").InputValueAsync());
        Assert.Equal(bodyFormatCode, await page.Locator("#editor-mode").InputValueAsync());
        Assert.True(await IsEditorFocusedAsync(page), $"Expected the {bodyFormatCode} editor to have focus.");

        await AssertEnabledAsync(page, "#checklist-new-text");
        await AssertEnabledAsync(page, "#checklist-add-button");
        await AssertEnabledAsync(page, "#attachment-file");
        await AssertEnabledAsync(page, "#attachment-add-button");
        await AssertEnabledAsync(page, "#comment-text");
        await AssertEnabledAsync(page, "#comment-add-button");
        await AssertEnabledAsync(page, "#relationship-type");
        await AssertEnabledAsync(page, "#relationship-task");
        await AssertEnabledAsync(page, "#relationship-add-button");
        await AssertEnabledAsync(page, "#complete-button");
        await AssertEnabledAsync(page, "#cancel-button");
        await page.Locator("#timeline-list").GetByText("Task created").WaitForAsync();

        await page.Locator("#checklist-new-text").FillAsync("Confirm the saved task can be extended");
        await page.Locator("#checklist-add-button").ClickAsync();
        var checklistText = page.Locator("#checklist-list .checklist-text");
        await checklistText.WaitForAsync();
        Assert.Equal("Confirm the saved task can be extended", await checklistText.InputValueAsync());

        await page.Locator("#attachment-file").SetInputFilesAsync(new FilePayload
        {
            Name = "saved-task-proof.txt",
            MimeType = "text/plain",
            Buffer = Encoding.UTF8.GetBytes("The new task is persisted before attachments are enabled.")
        });
        await page.Locator("#attachment-add-button").ClickAsync();
        await page.Locator("#attachment-list").GetByText("saved-task-proof.txt").WaitForAsync();

        await page.Locator("#comment-text").FillAsync("The task accepts a note immediately after creation.");
        await page.Locator("#comment-add-button").ClickAsync();
        await page.Locator("#timeline-list").GetByText("The task accepts a note immediately after creation.").WaitForAsync();

        var evidence = await fixture.ReadTaskEvidenceAsync(taskTitle);
        Assert.True(evidence.TaskId > 0);
        Assert.Equal(1, evidence.ChecklistItemCount);
        Assert.Equal(1, evidence.AttachmentCount);
        Assert.Equal(1, evidence.CommentCount);
        Assert.True(evidence.LogCount >= 4);
        Assert.Contains("task.create", fixture.BridgeMessageTypes);
        Assert.Contains("task.get", fixture.BridgeMessageTypes);
        Assert.DoesNotContain("task.update", fixture.BridgeMessageTypes);
    }

    [Fact]
    public async Task TaskTitleRail_UsesEditableTitleAsHeadingAndKeepsLifecycleBesideIt()
    {
        await using var fixture = await UiAppFixture.CreateAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1600, Height = 1000 }
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);

        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{fixture.BaseUrl}/index.html?v=task-title-rail-option-3",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-type option').length > 0");

        const string taskTitle = "Prepare release readiness checklist";
        await page.Locator("#new-task-button").ClickAsync();
        await page.Locator("#new-task-title-input").FillAsync(taskTitle);
        await page.Locator("#new-task-save-button").ClickAsync();
        await page.Locator("#new-task-overlay").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden
        });
        if (await page.Locator("#task-selection-coachmark").IsVisibleAsync())
        {
            await page.Locator("#task-selection-coachmark-dismiss").ClickAsync();
        }

        var title = page.Locator("#task-title");
        var status = page.Locator("#task-status-label");
        var rail = page.Locator(".task-title-rail");
        Assert.Equal(taskTitle, await title.InputValueAsync());
        Assert.Equal("Task title", await title.GetAttributeAsync("aria-label"));
        Assert.Equal(0, await page.Locator("label[for='task-title']").CountAsync());
        Assert.Equal(0, await page.Locator("#task-editor-title").CountAsync());
        Assert.Equal(
            "true",
            await rail.EvaluateAsync<string>(
                "element => String(element.children[0].id === 'task-title' && element.children[1].id === 'task-status-label')"));

        var railBox = await rail.BoundingBoxAsync();
        var titleBox = await title.BoundingBoxAsync();
        var statusBox = await status.BoundingBoxAsync();
        var metadataBox = await page.Locator(".metadata-grid").BoundingBoxAsync();
        var metadataFields = page.Locator(".metadata-grid > .field-block");
        Assert.NotNull(railBox);
        Assert.NotNull(titleBox);
        Assert.NotNull(statusBox);
        Assert.NotNull(metadataBox);
        Assert.InRange(railBox!.Height, 42, 74);
        Assert.InRange(Math.Abs((titleBox!.Y + titleBox.Height / 2) - (statusBox!.Y + statusBox.Height / 2)), 0, 8);
        Assert.InRange(statusBox.X - (titleBox.X + titleBox.Width), 0, 18);
        Assert.InRange(metadataBox!.Y - (railBox.Y + railBox.Height), 0, 20);
        Assert.Equal(
            2,
            await page.Locator(".metadata-grid").EvaluateAsync<int>(
                "element => getComputedStyle(element).gridTemplateColumns.split(' ').length"));
        Assert.Equal(6, await metadataFields.CountAsync());
        var firstRowFirstField = await metadataFields.Nth(0).BoundingBoxAsync();
        var firstRowLastField = await metadataFields.Nth(1).BoundingBoxAsync();
        var secondRowFirstField = await metadataFields.Nth(2).BoundingBoxAsync();
        var secondRowLastField = await metadataFields.Nth(3).BoundingBoxAsync();
        var thirdRowFirstField = await metadataFields.Nth(4).BoundingBoxAsync();
        var thirdRowLastField = await metadataFields.Nth(5).BoundingBoxAsync();
        Assert.NotNull(firstRowFirstField);
        Assert.NotNull(firstRowLastField);
        Assert.NotNull(secondRowFirstField);
        Assert.NotNull(secondRowLastField);
        Assert.NotNull(thirdRowFirstField);
        Assert.NotNull(thirdRowLastField);
        Assert.InRange(Math.Abs(firstRowFirstField!.Y - firstRowLastField!.Y), 0, 1);
        Assert.True(secondRowFirstField!.Y > firstRowFirstField.Y);
        Assert.InRange(Math.Abs(secondRowFirstField.Y - secondRowLastField!.Y), 0, 1);
        Assert.True(thirdRowFirstField!.Y > secondRowFirstField.Y);
        Assert.InRange(Math.Abs(thirdRowFirstField.Y - thirdRowLastField!.Y), 0, 1);
        var waitingInputBox = await page.Locator("#waiting-text").BoundingBoxAsync();
        var tagsInputBox = await page.Locator(".tags-field .select2-selection--multiple").BoundingBoxAsync();
        Assert.NotNull(waitingInputBox);
        Assert.NotNull(tagsInputBox);
        Assert.InRange(Math.Abs(waitingInputBox!.Height - tagsInputBox!.Height), 0, 1);

        var railStyles = await rail.EvaluateAsync<string[]>(
            """
            element => {
              const styles = getComputedStyle(element);
              return [styles.backgroundColor, styles.borderLeftWidth, styles.borderLeftStyle];
            }
            """);
        Assert.Equal("rgb(237, 247, 245)", railStyles[0]);
        Assert.Equal("3px", railStyles[1]);
        Assert.Equal("solid", railStyles[2]);
        await AssertNoHorizontalPageOverflowAsync(page);
        await CaptureWorkspaceAsync(page, "task-title-rail-option-3.png");

        await page.SetViewportSizeAsync(820, 900);
        await AssertNoHorizontalPageOverflowAsync(page);
        var compactRailBox = await rail.BoundingBoxAsync();
        var compactTitleBox = await title.BoundingBoxAsync();
        var compactStatusBox = await status.BoundingBoxAsync();
        Assert.NotNull(compactRailBox);
        Assert.NotNull(compactTitleBox);
        Assert.NotNull(compactStatusBox);
        Assert.True(compactTitleBox!.X + compactTitleBox.Width <= compactRailBox!.X + compactRailBox.Width + 1);
        Assert.True(compactStatusBox!.X + compactStatusBox.Width <= compactRailBox.X + compactRailBox.Width + 1);
        Assert.InRange(compactRailBox.Height, 40, 104);
        await CaptureWorkspaceAsync(page, "task-title-rail-option-3-compact.png");
    }

    [Fact]
    public async Task Help_DefaultsToDesktopGuideAndLoadsAllCanonicalTopics()
    {
        await using var fixture = await UiAppFixture.CreateAsync(seedSampleTasks: true);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1400, Height = 900 }
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);

        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{fixture.BaseUrl}/index.html?v=end-user-help",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-type option').length > 0");

        await page.Locator("#help-button").ClickAsync();
        await page.Locator("#help-overlay").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });

        var topicButtons = page.Locator(".help-topic-button");
        Assert.Equal(3, await topicButtons.CountAsync());
        Assert.Equal(
            "page",
            await page.Locator("[data-help-topic='using-okf-todo']").GetAttributeAsync("aria-current"));
        await page.Locator("#help-content h1").WaitForAsync();
        Assert.Equal("Use OKF-Todo Day to Day", await page.Locator("#help-content h1").TextContentAsync());
        await page.Locator("#help-content").GetByText("Start with one task", new LocatorGetByTextOptions
        {
            Exact = true
        }).WaitForAsync();

        await page.Locator("[data-help-topic='okf-layer']").ClickAsync();
        await page.Locator("#help-content h1").WaitForAsync();
        Assert.Equal(
            "Use the OKF Layer with an AI Assistant",
            await page.Locator("#help-content h1").TextContentAsync());
        var okfPrompt = page.Locator(".help-copy-block pre");
        await okfPrompt.WaitForAsync();
        var okfPromptText = await okfPrompt.TextContentAsync();
        Assert.Contains(
            Path.Combine("docs", "okf", "todo-database", "index.md"),
            okfPromptText);
        Assert.Contains(fixture.DatabasePath, okfPromptText);
        Assert.DoesNotContain("{{OKF_TODO_", okfPromptText);
        var copyPromptButton = page.Locator(".help-copy-button");
        Assert.Equal("Copy prompt", await copyPromptButton.TextContentAsync());
        await copyPromptButton.ClickAsync();
        Assert.Equal("Copied", await copyPromptButton.TextContentAsync());

        await page.Locator("[data-help-topic='mcp-server']").ClickAsync();
        await page.Locator("#help-content h1").WaitForAsync();
        Assert.Equal(
            "Use the MCP Server with Codex or Claude Code",
            await page.Locator("#help-content h1").TextContentAsync());

        await page.Locator("[data-help-topic='using-okf-todo']").ClickAsync();
        await page.Locator("#help-content h1").WaitForAsync();
        Assert.Equal("Use OKF-Todo Day to Day", await page.Locator("#help-content h1").TextContentAsync());
        await AssertNoHorizontalPageOverflowAsync(page);
        await CaptureViewportAsync(page, "help-using-okf-todo.png");
    }

    [Fact]
    public async Task SaveNewTask_RevealsSelectedTaskInsideQueueAndKeepsEditorFocused()
    {
        await using var fixture = await UiAppFixture.CreateAsync(seedSampleTasks: true);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1600, Height = 1000 }
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);

        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{fixture.BaseUrl}/index.html?v=new-task-reveal-contract",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-list .task-row').length > 10");
        await page.Locator("#task-sort").SelectOptionAsync("TITLE_ASC");
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#task-list')?.scrollHeight > document.querySelector('#task-list')?.clientHeight");
        await page.Locator("#task-list").EvaluateAsync("element => { element.scrollTop = 0; }");
        var expectedWorkspaceScrollPosition = await ReadWorkspaceScrollPositionAsync(page);

        const string taskTitle = "ZZZ newly revealed task";
        await page.Locator("#new-task-button").ClickAsync();
        await page.Locator("#new-task-title-input").FillAsync(taskTitle);
        await page.Locator("#new-task-save-button").ClickAsync();
        await page.Locator("#new-task-overlay").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden
        });

        await page.WaitForFunctionAsync(
            """
            expectedTitle => {
              const selectedRow = document.querySelector('#task-list .task-row.is-selected')
              return selectedRow?.querySelector('.task-row-title')?.textContent.trim() === expectedTitle
            }
            """,
            taskTitle);
        await page.WaitForFunctionAsync(
            """
            expectedTitle => {
              const taskList = document.querySelector('#task-list')
              const selectedRow = document.querySelector('#task-list .task-row.is-selected')
              if (!taskList || !selectedRow
                  || selectedRow.querySelector('.task-row-title')?.textContent.trim() !== expectedTitle) {
                return false
              }

              const listBox = taskList.getBoundingClientRect()
              const rowBox = selectedRow.getBoundingClientRect()
              return taskList.scrollTop > 0
                && rowBox.top >= listBox.top - 1
                && rowBox.bottom <= listBox.bottom + 1
            }
            """,
            taskTitle);

        Assert.Equal(taskTitle, await page.Locator("#task-title").InputValueAsync());
        Assert.True(await IsEditorFocusedAsync(page), "Expected the editor to keep keyboard focus after revealing the new task.");
        var actualWorkspaceScrollPosition = await ReadWorkspaceScrollPositionAsync(page);
        Assert.Equal(expectedWorkspaceScrollPosition.Length, actualWorkspaceScrollPosition.Length);
        for (var index = 0; index < expectedWorkspaceScrollPosition.Length; index++)
        {
            Assert.InRange(
                Math.Abs(actualWorkspaceScrollPosition[index] - expectedWorkspaceScrollPosition[index]),
                0,
                1);
        }
    }

    [Fact]
    public async Task MarkdownUnsavedChanges_SwitchingTasks_CanCancelOrSaveBeforeNavigation()
    {
        await using var fixture = await UiAppFixture.CreateAsync();
        await fixture.SendBridgeAsync("editor.preference.save", new
        {
            bodyFormatCode = "MARKDOWN",
            markdownEditType = "MARKDOWN",
            editorHeight = 360
        });
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1600, Height = 1000 }
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);

        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{fixture.BaseUrl}/index.html?v=markdown-unsaved-task-switch-contract",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-type option').length > 0");

        async Task CreateTaskAsync(string title)
        {
            await page.Locator("#new-task-button").ClickAsync();
            await page.Locator("#new-task-title-input").FillAsync(title);
            await page.Locator("#new-task-save-button").ClickAsync();
            await page.Locator("#new-task-overlay").WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden
            });
            await page.WaitForFunctionAsync(
                "expectedTitle => document.querySelector('#task-title')?.value === expectedTitle",
                title);
        }

        const string sourceTitle = "Markdown unsaved source";
        const string targetTitle = "Markdown unsaved target";
        const string changedMarkdown = "# Keep this Markdown\n\nThis must be saved before navigation.";
        await CreateTaskAsync(sourceTitle);
        await CreateTaskAsync(targetTitle);

        var sourceRow = page.Locator("#task-list .task-row")
            .Filter(new LocatorFilterOptions { HasText = sourceTitle });
        var targetRow = page.Locator("#task-list .task-row")
            .Filter(new LocatorFilterOptions { HasText = targetTitle });

        await sourceRow.ClickAsync();
        await page.WaitForFunctionAsync(
            "expectedTitle => document.querySelector('#task-title')?.value === expectedTitle",
            sourceTitle);
        await page.Locator("#editor-host .CodeMirror").First.WaitForAsync();
        await page.EvaluateAsync(
            "value => window.Editor.load(value)",
            changedMarkdown);

        await targetRow.ClickAsync();
        await page.Locator("#unsaved-changes-overlay").WaitForAsync();

        await page.Locator("#unsaved-cancel-button").ClickAsync();
        await page.Locator("#unsaved-changes-overlay").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden
        });
        Assert.Equal(sourceTitle, await page.Locator("#task-title").InputValueAsync());
        Assert.Equal(
            changedMarkdown,
            await page.EvaluateAsync<string>(
                "() => window.Editor.getMarkdown()"));

        await targetRow.ClickAsync();
        await page.Locator("#unsaved-changes-overlay").WaitForAsync();
        await page.Locator("#unsaved-save-button").ClickAsync();
        await page.WaitForFunctionAsync(
            "expectedTitle => document.querySelector('#task-title')?.value === expectedTitle",
            targetTitle);
        Assert.Contains("task.update", fixture.BridgeMessageTypes);
        Assert.Equal(changedMarkdown, await fixture.ReadTaskBodyAsync(sourceTitle));
    }

    [Fact]
    public async Task OwnershipFields_HaveIndependentPersistedVisibilityAndParticipateInOverviewSearch()
    {
        await using var fixture = await UiAppFixture.CreateAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1600, Height = 1000 }
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);

        var page = await context.NewPageAsync();
        await page.GotoAsync($"{fixture.BaseUrl}/index.html?v=ownership-fields-contract", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-type option').length > 0");
        Assert.Equal(
            "/css/app.css?v=ownership-fields-contract",
            await page.Locator("#app-stylesheet").GetAttributeAsync("href"));

        const string taskTitle = "Ownership search browser contract";
        await page.Locator("#new-task-button").ClickAsync();
        await page.Locator("#new-task-title-input").FillAsync(taskTitle);
        await page.Locator("#new-task-save-button").ClickAsync();
        await page.Locator("#new-task-overlay").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden
        });

        Assert.True(await page.Locator(".ownership-grid").IsHiddenAsync());
        Assert.True(await page.Locator(".owner-field").IsHiddenAsync());
        Assert.True(await page.Locator(".responsible-field").IsHiddenAsync());

        await OpenTaskDetailsPreferencesAsync(page);
        Assert.False(await page.Locator("#show-owner").IsCheckedAsync());
        Assert.False(await page.Locator("#show-responsible").IsCheckedAsync());

        await page.Locator("#show-owner").CheckAsync();
        Assert.True(await page.Locator("#show-owner").IsCheckedAsync());
        Assert.False(await page.Locator("#show-responsible").IsCheckedAsync());
        await WaitForDisplayPreferenceSavedAsync(page);
        await page.Locator("#settings-close-button").ClickAsync();

        Assert.False(await page.Locator(".ownership-grid").IsHiddenAsync());
        Assert.False(await page.Locator(".owner-field").IsHiddenAsync());
        Assert.True(await page.Locator(".responsible-field").IsHiddenAsync());

        await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-type option').length > 0");
        await page.Locator("#task-title").WaitForAsync();
        Assert.False(await page.Locator(".owner-field").IsHiddenAsync());
        Assert.True(await page.Locator(".responsible-field").IsHiddenAsync());

        await OpenTaskDetailsPreferencesAsync(page);
        Assert.True(await page.Locator("#show-owner").IsCheckedAsync());
        Assert.False(await page.Locator("#show-responsible").IsCheckedAsync());
        await page.Locator("#show-responsible").CheckAsync();
        Assert.True(await page.Locator("#show-owner").IsCheckedAsync());
        Assert.True(await page.Locator("#show-responsible").IsCheckedAsync());
        await WaitForDisplayPreferenceSavedAsync(page);
        await page.Locator("#settings-close-button").ClickAsync();

        await page.SetViewportSizeAsync(680, 1000);
        await page.WaitForFunctionAsync(
            """
            () => {
              const owner = document.querySelector('.owner-field')?.getBoundingClientRect()
              const responsible = document.querySelector('.responsible-field')?.getBoundingClientRect()
              return owner && responsible && Math.abs(owner.y - responsible.y) <= 1
            }
            """);
        var ownerBox = await page.Locator(".owner-field").BoundingBoxAsync();
        var responsibleBox = await page.Locator(".responsible-field").BoundingBoxAsync();
        Assert.NotNull(ownerBox);
        Assert.NotNull(responsibleBox);
        var ownershipLayout = await page.EvaluateAsync<string>(
            """
            () => {
              const grid = document.querySelector('.ownership-grid')
              const owner = document.querySelector('.owner-field')
              const responsible = document.querySelector('.responsible-field')
              return JSON.stringify({
                gridClass: grid?.className,
                gridDisplay: grid ? getComputedStyle(grid).display : null,
                gridColumns: grid ? getComputedStyle(grid).gridTemplateColumns : null,
                gridWidth: grid?.getBoundingClientRect().width,
                ownerHidden: owner?.hidden,
                ownerBox: owner?.getBoundingClientRect().toJSON(),
                responsibleHidden: responsible?.hidden,
                responsibleBox: responsible?.getBoundingClientRect().toJSON()
              })
            }
            """);
        Assert.True(
            Math.Abs(ownerBox.Y - responsibleBox.Y) <= 1,
            $"Ownership fields must share one row: {ownershipLayout}");
        Assert.True(ownerBox.X < responsibleBox.X);

        await page.Locator("#task-owner").FillAsync("North Support");
        await page.Locator("#task-responsible").FillAsync("Anna Jensen");
        await page.Locator("#save-button").ClickAsync();
        await page.WaitForFunctionAsync("() => document.querySelector('#save-status').textContent === 'Saved'");

        await page.Locator("#task-search").FillAsync("north support");
        await page.Locator("#task-list .task-row").GetByText(taskTitle).WaitForAsync();
        Assert.Single(await page.Locator("#task-list .task-row").AllAsync());

        await page.Locator("#task-search").FillAsync("anna jensen");
        await page.Locator("#task-list .task-row").GetByText(taskTitle).WaitForAsync();
        Assert.Single(await page.Locator("#task-list .task-row").AllAsync());

        await page.Locator("#task-search").FillAsync("");
        await OpenTaskDetailsPreferencesAsync(page);
        await page.Locator("#show-owner").UncheckAsync();
        Assert.False(await page.Locator("#show-owner").IsCheckedAsync());
        Assert.True(await page.Locator("#show-responsible").IsCheckedAsync());
        await WaitForDisplayPreferenceSavedAsync(page);
        await page.Locator("#settings-close-button").ClickAsync();
        Assert.True(await page.Locator(".owner-field").IsHiddenAsync());
        Assert.False(await page.Locator(".responsible-field").IsHiddenAsync());

        await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-type option').length > 0");
        Assert.True(await page.Locator(".owner-field").IsHiddenAsync());
        Assert.False(await page.Locator(".responsible-field").IsHiddenAsync());

        await OpenTaskDetailsPreferencesAsync(page);
        Assert.False(await page.Locator("#show-owner").IsCheckedAsync());
        Assert.True(await page.Locator("#show-responsible").IsCheckedAsync());
        await page.Locator("#show-responsible").UncheckAsync();
        await WaitForDisplayPreferenceSavedAsync(page);
        await page.Locator("#settings-close-button").ClickAsync();
        Assert.True(await page.Locator(".ownership-grid").IsHiddenAsync());
        Assert.True(await page.Locator(".owner-field").IsHiddenAsync());
        Assert.True(await page.Locator(".responsible-field").IsHiddenAsync());
    }

    [Fact]
    public async Task OptionalDetails_StayAboveBodyAndReadOnlyTasksUseCompactReviewPresentation()
    {
        await using var fixture = await UiAppFixture.CreateAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1487, Height = 1058 }
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);

        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{fixture.BaseUrl}/index.html?v=semantic-task-detail-slots",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-type option').length > 0");

        const string taskTitle = "Review optional task details";
        await page.Locator("#new-task-button").ClickAsync();
        await page.Locator("#new-task-title-input").FillAsync(taskTitle);
        await page.Locator("#new-task-save-button").ClickAsync();
        await page.Locator("#new-task-overlay").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden
        });

        await OpenTaskDetailsPreferencesAsync(page);
        await page.Locator("#show-source-fields").CheckAsync();
        await page.Locator("#show-owner").CheckAsync();
        await page.Locator("#show-responsible").CheckAsync();
        await page.Locator("#show-relationships").CheckAsync();
        await WaitForDisplayPreferenceSavedAsync(page);
        await page.Locator("#settings-close-button").ClickAsync();

        Assert.Equal(
            "true",
            await page.EvaluateAsync<string>(
                """
                () => {
                  const editableDetails = document.querySelector('#task-editable-details')
                  const editableBody = document.querySelector('#task-editable-body')
                  const ownership = document.querySelector('.ownership-grid')
                  const source = document.querySelector('.source-grid')
                  if (!editableDetails || !editableBody || !ownership || !source) return 'false'

                  const detailsBeforeBody = Boolean(
                    editableDetails.compareDocumentPosition(editableBody)
                      & Node.DOCUMENT_POSITION_FOLLOWING)
                  const ownershipBeforeBody = Boolean(
                    ownership.compareDocumentPosition(editableBody)
                      & Node.DOCUMENT_POSITION_FOLLOWING)
                  const sourceBeforeBody = Boolean(
                    source.compareDocumentPosition(editableBody)
                      & Node.DOCUMENT_POSITION_FOLLOWING)
                  return String(detailsBeforeBody && ownershipBeforeBody && sourceBeforeBody)
                }
                """));

        await page.Locator("#task-owner").FillAsync("Developer Support");
        await page.Locator("#task-responsible").FillAsync("Anna Jensen");
        await page.Locator("#task-source").SelectOptionAsync("EMAIL");
        await page.Locator("#task-source-reference").FillAsync("Customer thread 4711");
        await page.Locator("#task-source-url").FillAsync("https://example.invalid/customer-thread/4711");
        var longBody = string.Join(
            string.Empty,
            Enumerable.Range(1, 24)
                .Select(index => $"<p>Review detail {index}: preserve this information for the finished task.</p>"));
        await page.EvaluateAsync("value => window.Editor.load(value)", longBody);
        await page.Locator("#save-button").ClickAsync();
        await page.WaitForFunctionAsync("() => document.querySelector('#save-status')?.textContent === 'Saved'");

        await page.Locator("#complete-button").ClickAsync();
        await AssertCurrentTaskReadOnlyAsync(page, "Completed task — read only");

        Assert.True(await page.Locator("#task-editable-details").IsHiddenAsync());
        Assert.True(await page.Locator("#task-editable-body").IsHiddenAsync());
        Assert.True(await page.Locator("#task-read-only-details").IsVisibleAsync());
        Assert.True(await page.Locator("#task-read-only-body").IsVisibleAsync());
        Assert.True(await page.Locator("#editor-host").IsHiddenAsync());
        await page.Locator("#task-read-only-details").GetByText("Developer Support", new LocatorGetByTextOptions
        {
            Exact = true
        }).WaitForAsync();
        await page.Locator("#task-read-only-details").GetByText("Anna Jensen", new LocatorGetByTextOptions
        {
            Exact = true
        }).WaitForAsync();
        await page.Locator("#task-read-only-details").GetByText("Customer thread 4711", new LocatorGetByTextOptions
        {
            Exact = true
        }).WaitForAsync();
        await page.Locator("#task-read-only-body-content").GetByText(
            "Review detail 1: preserve this information for the finished task.",
            new LocatorGetByTextOptions { Exact = true }).WaitForAsync();

        Assert.True(await page.Locator(".relationship-add-form").IsHiddenAsync());
        Assert.True(await page.Locator(".checklist-add-form").IsHiddenAsync());
        Assert.True(await page.Locator(".attachment-actions").IsHiddenAsync());
        Assert.True(await page.Locator(".comment-form").IsHiddenAsync());

        var bodyToggle = page.Locator("#task-read-only-body-toggle");
        await bodyToggle.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        Assert.Equal("false", await bodyToggle.GetAttributeAsync("aria-expanded"));
        await bodyToggle.ClickAsync();
        Assert.Equal("true", await bodyToggle.GetAttributeAsync("aria-expanded"));
        Assert.True(await page.Locator("#task-read-only-body-content").EvaluateAsync<bool>(
            "element => element.classList.contains('is-expanded')"));

        await page.Locator("#task-read-only-reopen-button").ClickAsync();
        await AssertCurrentTaskEditableAsync(page);
        Assert.True(await page.Locator("#task-editable-details").IsVisibleAsync());
        Assert.True(await page.Locator("#task-editable-body").IsVisibleAsync());
        Assert.True(await page.Locator("#task-read-only-details").IsHiddenAsync());
        Assert.True(await page.Locator("#task-read-only-body").IsHiddenAsync());
    }

    [Fact]
    public async Task LifecycleActions_SwitchViewAndKeepChangedTaskSelectedRevealedAndFocused()
    {
        await using var fixture = await UiAppFixture.CreateAsync(seedSampleTasks: true);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1487, Height = 1058 }
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);

        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{fixture.BaseUrl}/index.html?v=lifecycle-destination-contract",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-type option').length > 0");

        const string taskTitle = "Lifecycle destination browser contract";
        await page.Locator("#new-task-button").ClickAsync();
        await page.Locator("#new-task-title-input").FillAsync(taskTitle);
        await page.Locator("#new-task-save-button").ClickAsync();
        await page.Locator("#new-task-overlay").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden
        });

        var workspaceScrollPosition = await ReadWorkspaceScrollPositionAsync(page);
        await page.Locator("#complete-button").ClickAsync();
        await AssertLifecycleDestinationAsync(
            page,
            taskTitle,
            "completed",
            "COMPLETED · VIEWING IN COMPLETED",
            "is-transition-completed",
            "Reopen",
            workspaceScrollPosition);
        await CaptureWorkspaceAsync(page, "lifecycle-destination-completed.png");

        workspaceScrollPosition = await ReadWorkspaceScrollPositionAsync(page);
        await page.Locator("#complete-button").ClickAsync();
        await AssertLifecycleDestinationAsync(
            page,
            taskTitle,
            "active",
            "ACTIVE · VIEWING IN ACTIVE",
            "is-transition-active",
            "Complete",
            workspaceScrollPosition);

        workspaceScrollPosition = await ReadWorkspaceScrollPositionAsync(page);
        await page.Locator("#cancel-button").ClickAsync();
        await AssertLifecycleDestinationAsync(
            page,
            taskTitle,
            "all",
            "CANCELLED · VIEWING IN ALL STATUSES",
            "is-transition-cancelled",
            "Reopen",
            workspaceScrollPosition);

        await page.SetViewportSizeAsync(1280, 800);
        Assert.Equal(
            new[] { "Any", "Active", "Completed", "Cancelled" },
            await page.Locator("#task-status-filter option").AllTextContentsAsync());
        Assert.True(await page.Locator("#task-filter-summary").IsHiddenAsync());
        Assert.Equal("0", await page.Locator("#task-filter-count-badge").TextContentAsync());
        var taskSearchBox = await page.Locator(".task-search-field").BoundingBoxAsync();
        var taskFilterMenuBox = await page.Locator(".task-filter-menu").BoundingBoxAsync();
        var taskSortBox = await page.Locator(".task-sort-field").BoundingBoxAsync();
        Assert.NotNull(taskSearchBox);
        Assert.NotNull(taskFilterMenuBox);
        Assert.NotNull(taskSortBox);
        Assert.True(taskFilterMenuBox.Y >= taskSearchBox.Y + taskSearchBox.Height);
        Assert.InRange(Math.Abs(taskFilterMenuBox.Y - taskSortBox.Y), 0, 1);
        Assert.True(taskFilterMenuBox.X < taskSortBox.X);
        await page.Locator("#task-filter-button").ClickAsync();
        Assert.False(await page.Locator("#task-filter-popover").IsHiddenAsync());
        await page.WaitForFunctionAsync(
            "() => document.activeElement?.id === 'task-type-filter'");
        Assert.Equal(
            "task-type-filter",
            await page.EvaluateAsync<string>("() => document.activeElement?.id"));
        var filterPopoverBox = await page.Locator("#task-filter-popover").BoundingBoxAsync();
        var taskSidebarBox = await page.Locator(".task-sidebar").BoundingBoxAsync();
        Assert.NotNull(filterPopoverBox);
        Assert.NotNull(taskSidebarBox);
        Assert.True(filterPopoverBox.X >= taskSidebarBox.X);
        Assert.True(
            filterPopoverBox.X + filterPopoverBox.Width
                <= taskSidebarBox.X + taskSidebarBox.Width);
        Assert.True(await page.EvaluateAsync<bool>(
            """
            () => {
              const popover = document.querySelector('#task-filter-popover')
              const rect = popover?.getBoundingClientRect()
              if (!popover || !rect) {
                return false
              }
              const hit = document.elementFromPoint(rect.left + 20, rect.top + 20)
              return hit === popover || popover.contains(hit)
            }
            """));
        await page.Locator("#task-status-filter").SelectOptionAsync("CANCELLED");
        await page.WaitForFunctionAsync(
            "() => document.querySelectorAll('#task-list .task-row').length > 0 && [...document.querySelectorAll('#task-list .task-row')].every(row => row.classList.contains('is-cancelled'))");
        Assert.Equal("1", await page.Locator("#task-filter-count-badge").TextContentAsync());
        Assert.False(await page.Locator("#task-filter-summary").IsHiddenAsync());
        Assert.Equal(
            "Status: Cancelled",
            await page.Locator("#task-filter-chips [data-filter='task-status']").TextContentAsync());
        await page.Locator("#task-filter-button").ClickAsync();
        Assert.True(await page.Locator("#task-filter-popover").IsHiddenAsync());
        await page.Locator("#task-filter-chips [data-filter='task-status']").ClickAsync();
        Assert.Equal(string.Empty, await page.Locator("#task-status-filter").InputValueAsync());
        Assert.Equal("0", await page.Locator("#task-filter-count-badge").TextContentAsync());
        Assert.True(await page.Locator("#task-filter-summary").IsHiddenAsync());

        Assert.Contains("task.complete", fixture.BridgeMessageTypes);
        Assert.Contains("task.reopen", fixture.BridgeMessageTypes);
        Assert.Contains("task.cancel", fixture.BridgeMessageTypes);
    }

    [Fact]
    public async Task FinalTaskEditingPreferences_LockEachStateIndependentlyAndKeepReopenAvailable()
    {
        await using var fixture = await UiAppFixture.CreateAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1487, Height = 1058 }
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);

        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{fixture.BaseUrl}/index.html?v=final-task-editing-preference-contract",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-type option').length > 0");

        const string taskTitle = "Final task editing browser contract";
        await page.Locator("#new-task-button").ClickAsync();
        await page.Locator("#new-task-title-input").FillAsync(taskTitle);
        await page.Locator("#new-task-save-button").ClickAsync();
        await page.Locator("#new-task-overlay").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden
        });

        await page.Locator("#complete-button").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#complete-button')?.textContent === 'Reopen'");
        await AssertCurrentTaskReadOnlyAsync(page, "Completed task — read only");

        await OpenTaskDetailsPreferencesAsync(page);
        Assert.False(await page.Locator("#allow-editing-completed-tasks").IsCheckedAsync());
        Assert.False(await page.Locator("#allow-editing-cancelled-tasks").IsCheckedAsync());
        await page.Locator("#allow-editing-completed-tasks").SetCheckedAsync(true);
        await page.Locator("#settings-close-button").ClickAsync();
        await AssertCurrentTaskEditableAsync(page);

        await OpenTaskDetailsPreferencesAsync(page);
        await page.Locator("#allow-editing-completed-tasks").SetCheckedAsync(false);
        await page.Locator("#settings-close-button").ClickAsync();
        await AssertCurrentTaskReadOnlyAsync(page, "Completed task — read only");

        await page.Locator("#task-read-only-reopen-button").ClickAsync();
        await AssertCurrentTaskEditableAsync(page);
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#complete-button')?.textContent === 'Complete'");

        await page.Locator("#cancel-button").ClickAsync();
        await AssertCurrentTaskReadOnlyAsync(page, "Cancelled task — read only");

        await OpenTaskDetailsPreferencesAsync(page);
        Assert.False(await page.Locator("#allow-editing-completed-tasks").IsCheckedAsync());
        Assert.False(await page.Locator("#allow-editing-cancelled-tasks").IsCheckedAsync());
        await page.Locator("#allow-editing-cancelled-tasks").SetCheckedAsync(true);
        await page.Locator("#settings-close-button").ClickAsync();
        await AssertCurrentTaskEditableAsync(page);

        await OpenTaskDetailsPreferencesAsync(page);
        await page.Locator("#allow-editing-cancelled-tasks").SetCheckedAsync(false);
        await page.Locator("#settings-close-button").ClickAsync();

        await AssertCurrentTaskReadOnlyAsync(page, "Cancelled task — read only");
        await page.Locator("#settings-button").ClickAsync();
        await page.Locator("[data-preference-section='general']").ClickAsync();
        await page.Locator("[data-preference-select='editor-mode'] [data-value='MARKDOWN']").ClickAsync();
        await page.Locator("#settings-close-button").ClickAsync();
        await AssertMarkdownEditorReadOnlyAsync(page);

        Assert.Contains("layout.preference.save", fixture.BridgeMessageTypes);
        Assert.Contains("task.complete", fixture.BridgeMessageTypes);
        Assert.Contains("task.reopen", fixture.BridgeMessageTypes);
        Assert.Contains("task.cancel", fixture.BridgeMessageTypes);
    }

    [Fact]
    public async Task TaskExport_UsesCurrentFilteredResultsInCurrentSortOrder()
    {
        await using var fixture = await UiAppFixture.CreateAsync(seedSampleTasks: true);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1487, Height = 1058 }
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);
        await context.AddInitScriptAsync(ClipboardAdapterScript);

        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{fixture.BaseUrl}/index.html?v=task-markdown-export-contract",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-type option').length > 0");
        await page.Locator(".task-view-rail-button[data-task-view='active']").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#task-list-title')?.textContent === 'Active'"
                + " && !document.querySelector('#task-export-button')?.hidden");
        await page.Locator("#task-selection-coachmark").WaitForAsync();
        await page.Locator("#task-selection-coachmark-dismiss").ClickAsync();
        await page.Locator("#task-selection-coachmark").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden
        });
        await page.Locator("#task-list-switcher").SelectOptionAsync("ALL");
        await page.Locator("#task-search").FillAsync("deployment");
        await page.Locator("#task-sort").SelectOptionAsync("TITLE_ASC");
        await page.Locator("#task-sort-direction").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => {"
                + " const rows = document.querySelectorAll('#task-list .task-row-title');"
                + " return rows.length >= 2 && rows.length < 30"
                + " && document.querySelector('#task-sort')?.value === 'TITLE_ASC'"
                + " && document.querySelector('#task-sort-direction')?.getAttribute('aria-label') === 'Sort descending';"
                + " }");
        var visibleTitles = await page.Locator("#task-list .task-row-title").AllTextContentsAsync();
        Assert.True(visibleTitles.Count >= 2);
        Assert.Equal(
            visibleTitles.OrderByDescending(title => title, StringComparer.OrdinalIgnoreCase),
            visibleTitles);

        await page.Locator("#task-export-button").ClickAsync();
        await page.Locator("#task-export-overlay").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        await page.WaitForFunctionAsync(
            $"() => document.querySelector('#task-export-current-count')?.textContent === '{visibleTitles.Count}'");
        Assert.Equal("Active in All lists", await page.Locator("#task-export-current-title").TextContentAsync());
        Assert.Contains(
            "Current search and filters",
            await page.Locator("#task-export-current-description").TextContentAsync());
        Assert.Contains(
            "Title, descending",
            await page.Locator("#task-export-current-description").TextContentAsync());
        Assert.Equal(0, await page.Locator("input[name='task-export-kind']").CountAsync());
        await page.Locator("#task-export-columns-none").ClickAsync();
        await page.Locator("#task-export-column-options input[value='TITLE']").CheckAsync();
        await page.Locator("#task-export-column-options input[value='STATUS']").CheckAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#task-export-columns-summary')?.textContent.startsWith('2 of')");
        await page.Locator("#task-export-confirm-button").ClickAsync();
        await page.Locator("#task-export-overlay").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden
        });

        var markdown = await fixture.ReadTaskExportAsync();
        Assert.Contains("- Scope: Active results in All lists", markdown);
        Assert.Contains("- Ordering: Title, descending", markdown);
        Assert.Contains("| Title | Status |", markdown);
        Assert.DoesNotContain("| ID |", markdown);
        Assert.DoesNotContain("| List |", markdown);
        Assert.DoesNotContain("| Type |", markdown);
        Assert.Contains("Verify deployment variable replacement", markdown);
        Assert.Contains("Fix failed production deployment", markdown);
        Assert.DoesNotContain("Prepare Power Platform release notes", markdown);
        Assert.True(
            markdown.IndexOf("Verify deployment variable replacement", StringComparison.Ordinal)
            < markdown.IndexOf("Fix failed production deployment", StringComparison.Ordinal));
        Assert.Contains("task.export.markdown", fixture.BridgeMessageTypes);
        Assert.Contains("task.export.columns.save", fixture.BridgeMessageTypes);

        await page.Locator("#task-export-button").ClickAsync();
        await page.Locator("#task-export-overlay").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        Assert.True(await page.Locator("#task-export-column-options input[value='TITLE']").IsCheckedAsync());
        Assert.True(await page.Locator("#task-export-column-options input[value='STATUS']").IsCheckedAsync());
        Assert.Equal(
            2,
            await page.Locator("#task-export-column-options input:checked").CountAsync());
        await page.Locator("#task-export-copy-html-button").ClickAsync();
        await page.Locator("#task-export-overlay").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden
        });
        using var clipboard = JsonDocument.Parse(await page.EvaluateAsync<string>(
            "() => JSON.stringify(window.__clipboardWrite)"));
        var clipboardHtml = clipboard.RootElement.GetProperty("html").GetString() ?? string.Empty;
        var clipboardPlainText = clipboard.RootElement.GetProperty("plainText").GetString() ?? string.Empty;
        Assert.Contains("<table", clipboardHtml);
        Assert.Contains("<th", clipboardHtml);
        Assert.Contains("Verify deployment variable replacement", clipboardHtml);
        Assert.DoesNotContain("Prepare Power Platform release notes", clipboardHtml);
        Assert.Contains("| Title | Status |", clipboardPlainText);
        Assert.Contains("task.export.html", fixture.BridgeMessageTypes);
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#save-status')?.textContent.includes('as HTML')");
    }

    [Fact]
    public async Task TriageCommandWorkspace_AdaptsAcrossLargeCompactAndSmallWindows()
    {
        await using var fixture = await UiAppFixture.CreateAsync(seedSampleTasks: true);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1487, Height = 1058 }
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);

        var page = await context.NewPageAsync();
        await page.GotoAsync($"{fixture.BaseUrl}/index.html?v=triage-command-responsive-contract", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-type option').length > 0");
        await page.Locator("#task-list .task-row").First.ClickAsync();
        await page.Locator(".tox-tinymce").WaitForAsync();
        Assert.Equal("Triage order", await page.Locator("#task-sort option[value='ATTENTION']").TextContentAsync());
        Assert.Equal("Status order", await page.Locator("#task-sort option[value='STATUS']").TextContentAsync());
        Assert.Contains("overdue, urgent, ready active work", await page.Locator("#task-sort-help").GetAttributeAsync("title"));
        await page.Locator("#task-sort").SelectOptionAsync("STATUS");
        Assert.Contains("Mainly useful in All statuses", await page.Locator("#task-sort-help").GetAttributeAsync("title"));
        await page.Locator("#task-sort").SelectOptionAsync("ATTENTION");

        var largeRail = await page.Locator(".task-view-rail").BoundingBoxAsync();
        var largeList = await page.Locator(".task-sidebar").BoundingBoxAsync();
        var largeEditor = await page.Locator(".task-editor-panel").BoundingBoxAsync();
        Assert.NotNull(largeRail);
        Assert.NotNull(largeList);
        Assert.NotNull(largeEditor);
        Assert.True(largeRail.Width >= 160);
        Assert.True(largeList.Width >= 360);
        Assert.True(largeRail.X < largeList.X);
        Assert.True(largeList.X < largeEditor.X);
        Assert.False(await page.Locator(".task-view-rail-label").First.IsHiddenAsync());
        Assert.True(await page.Locator(".task-view-compact").IsHiddenAsync());
        var semanticIconColors = await page.Locator(".task-view-rail-button .fluent-icon")
            .EvaluateAllAsync<string[]>("icons => icons.map(icon => getComputedStyle(icon).color)");
        Assert.True(
            semanticIconColors.Distinct(StringComparer.Ordinal).Count() >= 5,
            "Expected the task views to retain distinct semantic icon colors.");
        await AssertNoHorizontalPageOverflowAsync(page);
        await page.Locator(".task-view-rail-button[data-task-view='attention']").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#task-list-title').textContent === 'Attention' && document.querySelector('#task-view').value === 'attention'");
        await page.Locator(".task-view-rail-button[data-task-view='active']").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#task-list-title').textContent === 'Active' && document.querySelector('#task-view').value === 'active'");
        await CaptureWorkspaceAsync(page, "triage-command-large.png");

        await page.SetViewportSizeAsync(1100, 900);
        var mediumRail = await page.Locator(".task-view-rail").BoundingBoxAsync();
        Assert.NotNull(mediumRail);
        Assert.InRange(mediumRail.Width, 160, 190);
        Assert.False(await page.Locator(".task-view-rail-label").First.IsHiddenAsync());
        Assert.True(await page.Locator(".task-view-compact").IsHiddenAsync());
        await AssertNoHorizontalPageOverflowAsync(page);
        await CaptureWorkspaceAsync(page, "triage-command-compact.png");

        await page.SetViewportSizeAsync(820, 900);
        Assert.True(await page.Locator(".task-view-rail").IsHiddenAsync());
        Assert.False(await page.Locator(".task-view-compact").IsHiddenAsync());
        var stackedList = await page.Locator(".task-sidebar").BoundingBoxAsync();
        var stackedResizer = await page.Locator("#layout-resizer").BoundingBoxAsync();
        var stackedEditor = await page.Locator(".task-editor-panel").BoundingBoxAsync();
        Assert.NotNull(stackedList);
        Assert.NotNull(stackedResizer);
        Assert.NotNull(stackedEditor);
        Assert.True(stackedList.Y < stackedResizer.Y);
        Assert.True(stackedResizer.Y < stackedEditor.Y);
        await page.Locator("#task-list .task-row").First.WaitForAsync();
        var firstSmallTask = await page.Locator("#task-list .task-row").First.BoundingBoxAsync();
        Assert.NotNull(firstSmallTask);
        Assert.True(firstSmallTask.Y < stackedList.Y + stackedList.Height);
        await AssertNoHorizontalPageOverflowAsync(page);
        await CaptureWorkspaceAsync(page, "triage-command-small.png");

        await page.SetViewportSizeAsync(1487, 1058);
        await OpenAppearancePreferencesAsync(page);
        var layoutPreferencePanel = await page.Locator(".preferences-layout-control")
            .EvaluateAsync<string>("control => control.closest('[data-preference-panel]').dataset.preferencePanel");
        Assert.Equal("appearance", layoutPreferencePanel);
        Assert.True(await page.Locator("[data-preference-select='task-filter-layout'] .preference-choice[data-value='COMPACT']")
            .EvaluateAsync<bool>("button => button.classList.contains('is-selected')"));
        await page.Locator("[data-preference-select='task-filter-layout'] .preference-choice[data-value='EXPANDED']").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.documentElement.classList.contains('task-filter-layout-expanded') && document.querySelector('#save-status').textContent === 'Task filter layout saved'");
        await page.Locator("#settings-close-button").ClickAsync();

        Assert.True(await page.Locator("#task-filter-button").IsHiddenAsync());
        Assert.False(await page.Locator("#task-filter-popover").IsHiddenAsync());
        var expandedTagFilterBox = await page.Locator(".task-filter-popover-tags").BoundingBoxAsync();
        var expandedTypeFilterBox = await page.Locator(".task-filter-popover-field[for='task-type-filter']").BoundingBoxAsync();
        var expandedStatusFilterBox = await page.Locator(".task-filter-popover-field[for='task-status-filter']").BoundingBoxAsync();
        var expandedPriorityFilterBox = await page.Locator(".task-filter-popover-field[for='task-priority-filter']").BoundingBoxAsync();
        var expandedTypeSelectBox = await page.Locator("#task-type-filter").BoundingBoxAsync();
        var expandedStatusSelectBox = await page.Locator("#task-status-filter").BoundingBoxAsync();
        Assert.NotNull(expandedTagFilterBox);
        Assert.NotNull(expandedTypeFilterBox);
        Assert.NotNull(expandedStatusFilterBox);
        Assert.NotNull(expandedPriorityFilterBox);
        Assert.NotNull(expandedTypeSelectBox);
        Assert.NotNull(expandedStatusSelectBox);
        Assert.True(expandedTagFilterBox.Y <= expandedTypeFilterBox.Y);
        Assert.True(expandedTypeFilterBox.Y <= expandedStatusFilterBox.Y);
        Assert.True(expandedStatusFilterBox.Y <= expandedPriorityFilterBox.Y);
        Assert.InRange(Math.Abs(expandedTypeSelectBox.Height - expandedStatusSelectBox.Height), 0, 1);
        Assert.Equal(
            "task-filter-menu",
            await page.Locator("#task-filter-popover").EvaluateAsync<string>(
                "popover => popover.parentElement.className"));
        Assert.Equal(
            "static",
            await page.Locator("#task-filter-popover").EvaluateAsync<string>(
                "popover => getComputedStyle(popover).position"));

        await OpenAppearancePreferencesAsync(page);
        await page.Locator("[data-preference-select='task-filter-layout'] .preference-choice[data-value='COMPACT']").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => !document.documentElement.classList.contains('task-filter-layout-expanded') && document.querySelector('#save-status').textContent === 'Task filter layout saved'");
        await page.Locator(".preferences-layout-control .preference-choice[data-value='STACKED']").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.documentElement.classList.contains('layout-mode-stacked') && document.querySelector('#save-status').textContent === 'Layout preference saved'");
        await page.Locator("#settings-close-button").ClickAsync();

        Assert.True(await page.Locator(".task-view-rail").IsHiddenAsync());
        Assert.False(await page.Locator(".task-view-compact").IsHiddenAsync());
        var explicitStackedList = await page.Locator(".task-sidebar").BoundingBoxAsync();
        var explicitStackedResizer = await page.Locator("#layout-resizer").BoundingBoxAsync();
        var explicitStackedEditor = await page.Locator(".task-editor-panel").BoundingBoxAsync();
        var explicitStackedBody = await page.Locator(".editor-host").BoundingBoxAsync();
        Assert.NotNull(explicitStackedList);
        Assert.NotNull(explicitStackedResizer);
        Assert.NotNull(explicitStackedEditor);
        Assert.NotNull(explicitStackedBody);
        Assert.True(explicitStackedList.Y < explicitStackedResizer.Y);
        Assert.True(explicitStackedResizer.Y < explicitStackedEditor.Y);
        Assert.True(
            explicitStackedBody.Y < explicitStackedEditor.Y + explicitStackedEditor.Height,
            "Expected the body editor to remain visible without first scrolling the stacked detail panel.");
        Assert.InRange(explicitStackedList.Height, 220, 455);
        await AssertNoHorizontalPageOverflowAsync(page);
        await CaptureWorkspaceAsync(page, "triage-command-stacked.png");
    }

    [Fact]
    public async Task TaskSelectionHeader_IsDiscoverableAndCoachmarkIsRemembered()
    {
        await using var fixture = await UiAppFixture.CreateAsync(seedSampleTasks: true);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1295, Height = 900 }
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);

        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{fixture.BaseUrl}/index.html?v=selection-header-discoverability",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-list .task-row').length > 0");
        await page.Locator("#task-selection-coachmark").WaitForAsync();

        Assert.Equal("Select tasks", await page.Locator(".task-select-mode-label").TextContentAsync());
        Assert.Matches(
            @"^\d+ tasks$",
            await page.Locator("#task-list-header-count").TextContentAsync() ?? string.Empty);
        Assert.True(await page.Locator("#task-view-overflow-button").IsHiddenAsync());

        await page.Locator("#task-selection-coachmark-dismiss").ClickAsync();
        await page.Locator("#task-selection-coachmark").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden
        });
        await page.WaitForTimeoutAsync(150);
        Assert.Contains("layout.preference.save", fixture.BridgeMessageTypes);

        await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-list .task-row').length > 0");
        Assert.True(await page.Locator("#task-selection-coachmark").IsHiddenAsync());

        await page.Locator("#task-select-mode-button").ClickAsync();
        Assert.Equal("Done selecting", await page.Locator(".task-select-mode-label").TextContentAsync());
        Assert.Equal("0 selected", await page.Locator("#task-list-header-count").TextContentAsync());
        Assert.Equal("true", await page.Locator("#task-select-mode-button").GetAttributeAsync("aria-pressed"));
        Assert.Equal("Select all", await page.Locator(".task-selection-all span").TextContentAsync());

        await page.Locator(".task-row-select").First.CheckAsync();
        Assert.Equal("1 selected", await page.Locator("#task-list-header-count").TextContentAsync());

        await page.Locator("#task-selection-cancel").ClickAsync();
        Assert.Equal("Select tasks", await page.Locator(".task-select-mode-label").TextContentAsync());
        Assert.True(await page.Locator("#task-selection-bar").IsHiddenAsync());

        await page.Locator(".task-view-rail-button[data-task-view='all']").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => !document.querySelector('#task-view-overflow-button')?.hidden");
        Assert.Equal("More", await page.Locator(".task-view-overflow-label").TextContentAsync());

        await page.SetViewportSizeAsync(430, 820);
        Assert.Equal(
            "none",
            await page.Locator(".task-view-overflow-label")
                .EvaluateAsync<string>("element => getComputedStyle(element).display"));
        Assert.NotEqual(
            "none",
            await page.Locator(".task-view-overflow-compact")
                .EvaluateAsync<string>("element => getComputedStyle(element).display"));
        await AssertNoHorizontalPageOverflowAsync(page);
    }

    [Fact]
    public async Task StarTrashUndoAndBulkActions_WorkAcrossViewsWithoutHardDeletingByDefault()
    {
        await using var fixture = await UiAppFixture.CreateAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1487, Height = 1058 }
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);

        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{fixture.BaseUrl}/index.html?v=star-trash-bulk-contract",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-type option').length > 0");
        Assert.Equal(0, await page.Locator("#trash-permanent-actions").CountAsync());
        Assert.Equal(0, await page.Locator("#task-detail-star-button").CountAsync());
        Assert.Equal(0, await page.Locator("#task-detail-menu-button").CountAsync());
        Assert.True(await page.Locator("#task-detail-context-menu-button").IsHiddenAsync());
        Assert.True(await page.Locator("#task-view-overflow-button").IsHiddenAsync());

        const string starredTaskTitle = "Star and restore browser contract";
        const string bulkTaskTitle = "Bulk trash browser contract";
        const string deleteAllTaskTitle = "Delete all browser contract";
        foreach (var title in new[] { starredTaskTitle, bulkTaskTitle, deleteAllTaskTitle })
        {
            await page.Locator("#new-task-button").ClickAsync();
            await page.Locator("#new-task-title-input").FillAsync(title);
            await page.Locator("#new-task-save-button").ClickAsync();
            await page.Locator("#new-task-overlay").WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden
            });
        }

        var starredShell = page.Locator(".task-row-shell").Filter(new LocatorFilterOptions
        {
            HasText = starredTaskTitle
        });
        var starredTaskRow = starredShell.Locator(".task-row");
        await starredTaskRow.ClickAsync();
        await page.WaitForFunctionAsync(
            "title => document.querySelector('#task-title')?.value === title",
            starredTaskTitle);
        await starredShell.Locator(".task-row-star-button").ClickAsync();
        await page.WaitForFunctionAsync(
            "title => [...document.querySelectorAll('.task-row-shell')].find(shell => shell.textContent.includes(title))?.querySelector('.task-row-star-button')?.getAttribute('aria-pressed') === 'true'",
            starredTaskTitle);

        await page.Locator(".task-view-rail-button[data-task-view='starred']").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#task-list-title')?.textContent === 'Starred'");
        await page.Locator(".task-row").Filter(new LocatorFilterOptions { HasText = starredTaskTitle }).WaitForAsync();
        Assert.Single(await page.Locator("#task-list .task-row").AllAsync());

        await starredShell.Locator(".task-row-more").ClickAsync();
        await page.Locator("#task-action-menu [data-task-action='trash']").ClickAsync();
        await page.Locator("#task-undo-toast").WaitForAsync();
        await page.Locator("#task-list .empty-list").WaitForAsync();
        Assert.Contains("task.trash", fixture.BridgeMessageTypes);

        Assert.False(await page.Locator("#task-undo-button").IsDisabledAsync());
        await page.Locator("#task-undo-button").DispatchEventAsync("click");
        await page.Locator(".task-row").Filter(new LocatorFilterOptions { HasText = starredTaskTitle }).WaitForAsync();
        Assert.Contains("task.trash.restore", fixture.BridgeMessageTypes);

        await page.Locator(".task-view-rail-button[data-task-view='all']").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#task-list-title')?.textContent === 'All statuses'");
        Assert.True(await page.Locator("#task-view-overflow-button").IsHiddenAsync());
        await page.Locator("#task-select-mode-button").ClickAsync();
        await page.Locator("#task-select-all").CheckAsync();
        Assert.Equal("3 selected", await page.Locator("#task-list-header-count").TextContentAsync());
        Assert.False(await page.Locator("#task-bulk-trash").IsHiddenAsync());
        Assert.Equal("Move all selected to Trash", await page.Locator("#task-bulk-trash").TextContentAsync());
        Assert.True(await page.Locator("#task-bulk-delete-permanently").IsHiddenAsync());
        await page.Locator("#task-bulk-trash").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#save-status')?.textContent === 'Tasks moved to Trash' && document.querySelector('#task-list .empty-list')");

        await page.Locator(".task-view-rail-button[data-task-view='trash']").DispatchEventAsync("click");
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#task-list-title')?.textContent === 'Trash'");
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-list .task-row').length === 3");
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#complete-button')?.textContent === 'Restore'");
        Assert.Equal("Restore", await page.Locator("#complete-button").TextContentAsync());
        await AssertCurrentTaskReadOnlyAsync(page, "Task is in Trash", "Restore");
        Assert.False(await page.Locator("#task-view-overflow-button").IsHiddenAsync());
        Assert.False(await page.Locator("#task-view-overflow-button").IsDisabledAsync());
        Assert.True(await page.Locator("#task-detail-context-menu-button").IsHiddenAsync());

        var trashedStarredShell = page.Locator(".task-row-shell").Filter(new LocatorFilterOptions
        {
            HasText = starredTaskTitle
        });
        Assert.Equal(
            "Starred before moved to Trash",
            await trashedStarredShell.Locator(".task-row-star-static").GetAttributeAsync("title"));
        var trashedUnstarredShell = page.Locator(".task-row-shell").Filter(new LocatorFilterOptions
        {
            HasText = bulkTaskTitle
        });
        Assert.Equal(0, await trashedUnstarredShell.Locator(".task-row-star").CountAsync());

        await page.SetViewportSizeAsync(1295, 734);
        await CaptureWorkspaceAsync(page, "trash-bulk-actions.png");
        await page.Locator("#task-view-overflow-button").ClickAsync();
        Assert.False(await page.Locator("#task-view-overflow-menu").IsHiddenAsync());
        Assert.Contains("Empty Trash", await page.Locator("#trash-empty-button").TextContentAsync());
        await CaptureWorkspaceAsync(page, "trash-empty-menu-open.png");
        await page.Keyboard.PressAsync("Escape");
        Assert.True(await page.Locator("#task-view-overflow-menu").IsHiddenAsync());
        Assert.Equal("false", await page.Locator("#task-view-overflow-button").GetAttributeAsync("aria-expanded"));

        await page.SetViewportSizeAsync(820, 900);
        Assert.False(await page.Locator("#task-detail-context-menu-button").IsHiddenAsync());
        await page.Locator("#task-detail-context-menu-button").ClickAsync();
        Assert.True(await page.Locator("#task-action-menu [data-task-action='toggle-star']").IsHiddenAsync());
        Assert.False(await page.Locator("#task-action-menu [data-task-action='restore']").IsHiddenAsync());
        Assert.False(await page.Locator("#task-action-menu [data-task-action='delete-permanently']").IsHiddenAsync());
        await page.Keyboard.PressAsync("Escape");
        await page.Locator("#task-select-mode-button").ClickAsync();
        Assert.True(await page.Locator("#task-view-overflow-button").IsHiddenAsync());
        await page.Locator(".task-row-shell")
            .Filter(new LocatorFilterOptions { HasText = bulkTaskTitle })
            .Locator(".task-row-select")
            .CheckAsync();
        var selectionBarPosition = await page.Locator("#task-selection-bar")
            .EvaluateAsync<string>("element => getComputedStyle(element).position");
        Assert.Equal("fixed", selectionBarPosition);
        Assert.True(await page.Locator("#task-bulk-trash").IsHiddenAsync());
        Assert.False(await page.Locator("#task-bulk-delete-permanently").IsHiddenAsync());
        Assert.Equal(
            "Delete all selected permanently",
            await page.Locator("#task-bulk-delete-permanently").TextContentAsync());

        await page.Locator("#task-bulk-delete-permanently").ClickAsync();
        await page.Locator("#confirmation-overlay").WaitForAsync();
        Assert.Equal("Delete permanently", await page.Locator("#confirmation-confirm-button").TextContentAsync());
        await page.Locator("#confirmation-confirm-button").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#save-status')?.textContent === 'Task deleted permanently' && document.querySelectorAll('#task-list .task-row').length === 2");
        Assert.Contains("task.trash.delete", fixture.BridgeMessageTypes);

        await page.Locator(".task-row").Filter(new LocatorFilterOptions { HasText = starredTaskTitle }).ClickAsync();
        await page.Locator("#task-read-only-reopen-button").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#task-list-title')?.textContent === 'Active' && !document.querySelector('#task-title')?.disabled");
        Assert.Contains("task.trash.restore", fixture.BridgeMessageTypes);
        Assert.True(await page.Locator("#task-view-overflow-button").IsHiddenAsync());
        var restoredStarButton = page.Locator(".task-row-shell")
            .Filter(new LocatorFilterOptions { HasText = starredTaskTitle })
            .Locator(".task-row-star-button");
        Assert.Equal("true", await restoredStarButton.GetAttributeAsync("aria-pressed"));
        Assert.False(await page.Locator("#task-detail-context-menu-button").IsHiddenAsync());
        await page.Locator("#task-detail-context-menu-button").ClickAsync();
        Assert.False(await page.Locator("#task-action-menu [data-task-action='toggle-star']").IsHiddenAsync());
        Assert.Equal(
            "Unstar task",
            await page.Locator("#task-action-menu [data-task-action='toggle-star'] span:last-child").TextContentAsync());
        Assert.False(await page.Locator("#task-action-menu [data-task-action='trash']").IsHiddenAsync());
        Assert.True(await page.Locator("#task-action-menu [data-task-action='delete-permanently']").IsHiddenAsync());
        await page.Locator("#task-action-menu [data-task-action='toggle-star']").ClickAsync();
        await page.WaitForFunctionAsync(
            "title => document.querySelector('#save-status')?.textContent === 'Star removed' && [...document.querySelectorAll('.task-row-shell')].find(shell => shell.textContent.includes(title))?.querySelector('.task-row-star-button')?.getAttribute('aria-pressed') === 'false'",
            starredTaskTitle);
        await page.Locator("#task-detail-context-menu-button").ClickAsync();
        Assert.Equal(
            "Star task",
            await page.Locator("#task-action-menu [data-task-action='toggle-star'] span:last-child").TextContentAsync());
        await page.Locator("#task-action-menu [data-task-action='toggle-star']").ClickAsync();
        await page.WaitForFunctionAsync(
            "title => document.querySelector('#save-status')?.textContent === 'Task starred' && [...document.querySelectorAll('.task-row-shell')].find(shell => shell.textContent.includes(title))?.querySelector('.task-row-star-button')?.getAttribute('aria-pressed') === 'true'",
            starredTaskTitle);
        await page.Locator(".task-view-rail-button[data-task-view='trash']").DispatchEventAsync("click");
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#task-list-title')?.textContent === 'Trash' && document.querySelectorAll('#task-list .task-row').length === 1");
        await page.Locator("#task-search").FillAsync("hide every trashed task");
        await page.Locator("#task-list .empty-list").WaitForAsync();
        Assert.False(await page.Locator("#task-view-overflow-button").IsHiddenAsync());
        Assert.False(await page.Locator("#task-view-overflow-button").IsDisabledAsync());

        await page.Locator("#task-view-overflow-button").ClickAsync();
        await page.Locator("#trash-empty-button").ClickAsync();
        await page.Locator("#confirmation-overlay").WaitForAsync();
        Assert.Equal(
            "Permanently delete 1 task?",
            await page.Locator("#confirmation-title").TextContentAsync());
        Assert.Equal("Empty Trash", await page.Locator("#confirmation-confirm-button").TextContentAsync());
        Assert.Contains(
            "including items hidden by current search or filters",
            await page.Locator("#confirmation-message").TextContentAsync());
        await page.Locator("#confirmation-confirm-button").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#task-view-overflow-button')?.hidden && document.querySelector('#task-list .empty-list')");
        Assert.True(await page.Locator("#task-view-overflow-button").IsHiddenAsync());
    }

    [Fact]
    public async Task SelectionActions_MoveSelectedTasksAndAllCanMoveEveryCancelledTask()
    {
        await using var fixture = await UiAppFixture.CreateAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1295, Height = 900 }
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);

        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{fixture.BaseUrl}/index.html?v=view-wide-trash-actions-contract",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-type option').length > 0");

        const string selectedTitle = "Move selected task from Active";
        const string activeTitle = "Keep active task outside bulk Trash";
        const string cancelledTitleOne = "Move first cancelled task from All";
        const string cancelledTitleTwo = "Move second cancelled task from All";
        foreach (var title in new[] { selectedTitle, activeTitle, cancelledTitleOne, cancelledTitleTwo })
        {
            await page.Locator("#new-task-button").ClickAsync();
            await page.Locator("#new-task-title-input").FillAsync(title);
            await page.Locator("#new-task-save-button").ClickAsync();
            await page.Locator("#new-task-overlay").WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden
            });
        }

        Assert.True(await page.Locator("#task-view-overflow-button").IsHiddenAsync());
        await page.Locator("#task-search").FillAsync(selectedTitle);
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#task-list-header-count')?.textContent === '1 task'");
        await page.Locator("#task-select-mode-button").ClickAsync();
        await page.Locator(".task-row-select").CheckAsync();
        Assert.Equal(
            "Move all selected to Trash",
            await page.Locator("#task-bulk-trash").TextContentAsync());
        await page.Locator("#task-bulk-trash").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#save-status')?.textContent === 'Task moved to Trash'");
        Assert.Contains("task.trash", fixture.BridgeMessageTypes);

        await page.Locator("#task-undo-button").ClickAsync(new LocatorClickOptions { Force = true });
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#save-status')?.textContent === 'Task restored'");
        await page.Locator("#task-search").FillAsync("");

        foreach (var title in new[] { cancelledTitleOne, cancelledTitleTwo })
        {
            await page.Locator(".task-row").Filter(new LocatorFilterOptions { HasText = title }).ClickAsync();
            await page.WaitForFunctionAsync(
                "taskTitle => document.querySelector('#task-title')?.value === taskTitle",
                title);
            await page.Locator("#cancel-button").ClickAsync();
            await page.WaitForFunctionAsync(
                "taskTitle => document.querySelector('#task-list-title')?.textContent === 'All statuses' && document.querySelector('#task-title')?.value === taskTitle && document.querySelector('#complete-button')?.textContent === 'Reopen'",
                title);
        }

        await page.Locator("#task-search").FillAsync(activeTitle);
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#task-list-header-count')?.textContent === '1 task'");
        await page.Locator("#task-view-overflow-button").ClickAsync();
        Assert.False(await page.Locator("#task-view-move-cancelled").IsHiddenAsync());
        Assert.Contains(
            "Move all cancelled to Trash (2)",
            await page.Locator("#task-view-move-cancelled").TextContentAsync());

        await page.Locator("#task-view-move-cancelled").ClickAsync();
        await page.Locator("#confirmation-overlay").WaitForAsync();
        Assert.Equal(
            "Move 2 cancelled tasks to Trash?",
            await page.Locator("#confirmation-title").TextContentAsync());
        Assert.Contains(
            "all 2 cancelled tasks in All statuses",
            await page.Locator("#confirmation-message").TextContentAsync());
        await page.Locator("#confirmation-confirm-button").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#save-status')?.textContent === 'Tasks moved to Trash'");

        await page.Locator("#task-search").FillAsync("");
        await page.Locator(".task-view-rail-button[data-task-view='trash']").DispatchEventAsync("click");
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#task-list-title')?.textContent === 'Trash' && document.querySelectorAll('#task-list .task-row').length === 2");
        await page.Locator("#task-view-overflow-button").ClickAsync();
        Assert.Equal(0, await page.Locator("#task-view-move-starred").CountAsync());
        Assert.True(await page.Locator("#task-view-move-cancelled").IsHiddenAsync());
        Assert.False(await page.Locator("#trash-empty-button").IsHiddenAsync());
        await page.Keyboard.PressAsync("Escape");
        await page.Locator("#task-select-mode-button").ClickAsync();
        await page.Locator(".task-row-select").First.CheckAsync();
        Assert.Equal(
            "Delete all selected permanently",
            await page.Locator("#task-bulk-delete-permanently").TextContentAsync());
    }

    [Fact]
    public async Task TaskLists_CanBeManagedSelectedSearchedMovedAndUndone()
    {
        await using var fixture = await UiAppFixture.CreateAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1600, Height = 1000 },
            DeviceScaleFactor = 1
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);
        var page = await context.NewPageAsync();
        var consoleErrors = new ConcurrentQueue<string>();
        page.Console += (_, message) =>
        {
            if (message.Type == "error")
            {
                consoleErrors.Enqueue(message.Text);
            }
        };
        await page.GotoAsync($"{fixture.BaseUrl}/index.html?v=task-lists-contract");
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-list-switcher option').length === 2");

        await page.Locator("#manage-task-lists-button").ClickAsync();
        await page.Locator("#task-lists-overlay").WaitForAsync();
        Assert.Equal(
            "Default list",
            await page.Locator(".task-list-manager-row.is-selected .task-list-manager-name").TextContentAsync());
        Assert.Equal("Default list", await page.Locator("#task-list-detail-name").InputValueAsync());
        Assert.False(await page.Locator("#task-list-delete-guidance").IsHiddenAsync());
        Assert.True(await page.Locator("#task-list-delete-button").IsDisabledAsync());

        await page.Locator("#task-list-new-button").ClickAsync();
        Assert.Equal("New list", await page.Locator("#task-list-detail-mode").TextContentAsync());
        Assert.True(await page.Locator("#task-list-danger-zone").IsHiddenAsync());
        await page.Locator("#task-list-detail-name").FillAsync("Support draft");
        Assert.False(await page.Locator("#task-list-detail-save").IsDisabledAsync());
        await page.Locator("#task-list-detail-save").ClickAsync();
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-list-switcher option').length === 3");

        Assert.Equal("Support draft", await page.Locator("#task-list-detail-name").InputValueAsync());
        await page.Locator("#task-list-detail-name").FillAsync("Support");
        Assert.Equal("Save changes", await page.Locator("#task-list-detail-save").TextContentAsync());
        await page.Locator("#task-list-detail-save").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => [...document.querySelectorAll('#task-list-switcher option')].some(option => option.textContent === 'Support')");

        await page.Locator("#task-list-new-button").ClickAsync();
        await page.Locator("#task-list-detail-name").FillAsync("Temporary");
        await page.Locator("#task-list-detail-save").ClickAsync();
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-list-switcher option').length === 4");
        await page.Locator(".task-list-manager-row")
            .Filter(new LocatorFilterOptions { HasText = "Default list" })
            .Locator(".task-list-manager-select")
            .ClickAsync();
        await CaptureViewportAsync(page, "task-list-manager-master-detail.png");
        await page.Locator(".task-list-manager-row")
            .Filter(new LocatorFilterOptions { HasText = "Temporary" })
            .Locator(".task-list-manager-select")
            .ClickAsync();
        await page.Locator("#task-list-delete-button").ClickAsync();
        await page.Locator("#task-list-delete-overlay").WaitForAsync();
        Assert.Equal("Delete list", await page.Locator("#task-list-delete-confirm").TextContentAsync());
        await page.Locator("#task-list-delete-confirm").ClickAsync();
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-list-switcher option').length === 3");

        var supportListRow = page.Locator(".task-list-manager-row")
            .Filter(new LocatorFilterOptions { HasText = "Support" });
        var defaultListRow = page.Locator(".task-list-manager-row")
            .Filter(new LocatorFilterOptions { HasText = "Default list" });
        await supportListRow.DragToAsync(defaultListRow);
        await page.WaitForFunctionAsync("() => document.querySelector('#save-status')?.textContent === 'List order saved'");
        Assert.Equal(
            "Support",
            await page.Locator(".task-list-manager-row").First.Locator(".task-list-manager-name").TextContentAsync());

        await page.SetViewportSizeAsync(700, 900);
        await page.WaitForFunctionAsync(
            """
            () => {
              const listPane = document.querySelector('.task-list-manager-pane')?.getBoundingClientRect()
              const detailPane = document.querySelector('.task-list-detail-pane')?.getBoundingClientRect()
              return listPane && detailPane && detailPane.top >= listPane.bottom - 1
            }
            """);
        await AssertNoHorizontalPageOverflowAsync(page);
        await CaptureViewportAsync(page, "task-list-manager-master-detail-compact.png");
        await page.SetViewportSizeAsync(1600, 1000);

        await page.Locator("#task-lists-close-button").ClickAsync();

        await page.Locator("#task-list-switcher").SelectOptionAsync("ALL");
        await page.Locator("#new-task-button").ClickAsync();
        await page.Locator("#new-task-title-input").FillAsync("List-aware support task");
        Assert.False(await page.Locator("#new-task-list-field").IsHiddenAsync());
        await page.Locator("#new-task-list-select").SelectOptionAsync(
            new SelectOptionValue { Label = "Support" });
        await page.Locator("#new-task-save-button").ClickAsync();
        await page.Locator("#new-task-overlay").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden
        });

        Assert.Equal("Support", await page.Locator("#task-list-owner option:checked").TextContentAsync());
        Assert.Equal("Support", await page.Locator(".task-row.is-selected .task-list-pill").TextContentAsync());

        await page.Locator("#task-search").FillAsync("Support");
        Assert.Equal(1, await page.Locator(".task-row-shell").CountAsync());
        await page.Locator("#task-search").FillAsync(string.Empty);

        var supportTaskRow = page.Locator(".task-row-shell").Filter(
            new LocatorFilterOptions { HasText = "List-aware support task" });
        await supportTaskRow.Locator(".task-row-more").ClickAsync();
        var moveToListAction = page.Locator("#task-action-menu [data-task-action='move-list']");
        Assert.False(await moveToListAction.IsHiddenAsync());
        Assert.Equal("Move to list", await moveToListAction.Locator("span:last-child").TextContentAsync());
        await CaptureWorkspaceAsync(page, "task-menu-move-to-list.png");
        await moveToListAction.ClickAsync();
        Assert.Equal("Move task to list", await page.Locator("#task-list-move-title").TextContentAsync());
        Assert.Equal(1, await page.Locator("#task-list-move-destination option").CountAsync());
        Assert.Equal("Default list", await page.Locator("#task-list-move-destination option").TextContentAsync());
        await page.Locator("#task-list-move-confirm").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('.task-row.is-selected .task-list-pill')?.textContent.trim() === 'Default list'");
        await page.Locator("#task-undo-button").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('.task-row.is-selected .task-list-pill')?.textContent.trim() === 'Support'");

        await page.Locator("#task-list-switcher").SelectOptionAsync(
            new SelectOptionValue { Label = "Support" });
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#task-list-switcher option:checked')?.textContent === 'Support' && document.querySelector('#task-title')?.value === 'List-aware support task'");
        await page.Locator(".task-row-shell .task-row-more").ClickAsync();
        await page.Locator("#task-action-menu [data-task-action='move-list']").ClickAsync();
        await page.Locator("#task-list-move-confirm").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#task-list-switcher option:checked')?.textContent === 'Default list' && document.querySelector('#task-title')?.value === 'List-aware support task'");
        await page.Locator("#task-undo-button").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#task-list-switcher option:checked')?.textContent === 'Support' && document.querySelector('#task-title')?.value === 'List-aware support task'");
        await page.Locator("#task-list-switcher").SelectOptionAsync("ALL");

        await page.Locator("#task-select-mode-button").ClickAsync();
        await page.Locator(".task-row-select").CheckAsync();
        await page.Locator("#task-bulk-move-list").ClickAsync();
        await page.Locator("#task-list-move-destination").SelectOptionAsync(
            new SelectOptionValue { Label = "Default list" });
        await page.Locator("#task-list-move-confirm").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('.task-row.is-selected .task-list-pill')?.textContent.trim() === 'Default list'");
        await page.Locator("#task-undo-button").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('.task-row.is-selected .task-list-pill')?.textContent.trim() === 'Support'");

        var largeBrandBox = await page.Locator(".app-brand").BoundingBoxAsync();
        var largeSwitcherBox = await page.Locator(".task-list-switcher-group").BoundingBoxAsync();
        var largeRailBox = await page.Locator(".task-view-rail").BoundingBoxAsync();
        Assert.NotNull(largeBrandBox);
        Assert.NotNull(largeSwitcherBox);
        Assert.NotNull(largeRailBox);
        Assert.InRange(Math.Abs(largeSwitcherBox!.Y - largeBrandBox!.Y), 0, 8);
        Assert.InRange(
            Math.Abs(largeSwitcherBox.X - (largeRailBox!.X + largeRailBox.Width)),
            0,
            1);
        Assert.Equal(
            "rgb(242, 249, 248)",
            await page.Locator(".task-list-switcher-group").EvaluateAsync<string>(
                "element => getComputedStyle(element).backgroundColor"));
        Assert.Equal("Manage", await page.Locator("#manage-task-lists-button span:last-child").TextContentAsync());
        Assert.False(await page.Locator(".app-action-divider").IsHiddenAsync());
        var largeActionFontSize = await page.Locator("#new-task-button").EvaluateAsync<double>(
            "element => Number.parseFloat(getComputedStyle(element).fontSize)");
        await AssertNoHorizontalPageOverflowAsync(page);
        await CaptureWorkspaceAsync(page, "workspace-capsule-large.png");

        await page.SetViewportSizeAsync(1280, 800);
        var densityBrandBox = await page.Locator(".app-brand").BoundingBoxAsync();
        var densitySwitcherBox = await page.Locator(".task-list-switcher-group").BoundingBoxAsync();
        var densityRailBox = await page.Locator(".task-view-rail").BoundingBoxAsync();
        Assert.NotNull(densityBrandBox);
        Assert.NotNull(densitySwitcherBox);
        Assert.NotNull(densityRailBox);
        Assert.InRange(Math.Abs(densitySwitcherBox!.Y - densityBrandBox!.Y), 0, 8);
        Assert.InRange(
            Math.Abs(densitySwitcherBox.X - (densityRailBox!.X + densityRailBox.Width)),
            0,
            1);
        var densityActionFontSize = await page.Locator("#new-task-button").EvaluateAsync<double>(
            "element => Number.parseFloat(getComputedStyle(element).fontSize)");
        Assert.True(densityActionFontSize < largeActionFontSize);
        await AssertNoHorizontalPageOverflowAsync(page);
        await CaptureViewportAsync(page, "workspace-capsule-density-compact.png");

        await page.SetViewportSizeAsync(1200, 900);
        var mediumBrandBox = await page.Locator(".app-brand").BoundingBoxAsync();
        var mediumSwitcherBox = await page.Locator(".task-list-switcher-group").BoundingBoxAsync();
        Assert.NotNull(mediumBrandBox);
        Assert.NotNull(mediumSwitcherBox);
        Assert.True(mediumSwitcherBox!.Y >= mediumBrandBox!.Y + mediumBrandBox.Height - 1);
        await AssertNoHorizontalPageOverflowAsync(page);
        await CaptureWorkspaceAsync(page, "workspace-capsule-medium.png");

        await page.SetViewportSizeAsync(820, 900);
        var brandBox = await page.Locator(".app-brand").BoundingBoxAsync();
        var switcherBox = await page.Locator(".task-list-switcher-group").BoundingBoxAsync();
        Assert.NotNull(brandBox);
        Assert.NotNull(switcherBox);
        Assert.True(switcherBox!.Y >= brandBox!.Y + brandBox.Height - 1);
        await AssertNoHorizontalPageOverflowAsync(page);
        await CaptureWorkspaceAsync(page, "workspace-capsule-compact.png");

        await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#task-list-switcher')?.value === 'ALL'");

        Assert.Contains("taskList.create", fixture.BridgeMessageTypes);
        Assert.Contains("taskList.rename", fixture.BridgeMessageTypes);
        Assert.Contains("taskList.reorder", fixture.BridgeMessageTypes);
        Assert.Contains("taskList.delete", fixture.BridgeMessageTypes);
        Assert.Contains("taskList.moveTasks", fixture.BridgeMessageTypes);
        Assert.Contains("taskList.undoMove", fixture.BridgeMessageTypes);
        Assert.Contains("layout.preference.save", fixture.BridgeMessageTypes);
        Assert.Empty(consoleErrors);
    }

    private static async Task OpenTaskDetailsPreferencesAsync(IPage page)
    {
        await page.Locator("#settings-button").ClickAsync();
        await page.Locator("[data-preference-section='task-details']").ClickAsync();
    }

    private static async Task OpenAppearancePreferencesAsync(IPage page)
    {
        await page.Locator("#settings-button").ClickAsync();
        await page.Locator("[data-preference-section='appearance']").ClickAsync();
    }

    private static Task WaitForDisplayPreferenceSavedAsync(IPage page)
    {
        return page.WaitForFunctionAsync(
            "() => document.querySelector('#save-status').textContent === 'Display preference saved'");
    }

    private static async Task AssertEnabledAsync(IPage page, string selector)
    {
        Assert.False(await page.Locator(selector).IsDisabledAsync(), $"Expected {selector} to be enabled.");
    }

    private static async Task AssertNoHorizontalPageOverflowAsync(IPage page)
    {
        Assert.True(await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
    }

    private static async Task AssertLifecycleDestinationAsync(
        IPage page,
        string taskTitle,
        string view,
        string headerLabel,
        string transitionClass,
        string lifecycleButtonText,
        double[] expectedWorkspaceScrollPosition)
    {
        try
        {
            await page.WaitForFunctionAsync(
                """
            ({ title, view, headerLabel, transitionClass }) => {
              const row = [...document.querySelectorAll('#task-list .task-row')]
                .find(candidate => candidate.querySelector('.task-row-title')?.textContent === title)
              const list = document.querySelector('#task-list')
              const activeView = document.querySelector(`.task-view-rail-button[data-task-view="${view}"]`)
              const expectedListTitle = {
                active: 'Active',
                completed: 'Completed',
                all: 'All statuses'
              }[view]
              if (!row || !list || !activeView) return false

              const rowBox = row.getBoundingClientRect()
              const listBox = list.getBoundingClientRect()
              return document.querySelector('#task-list-title')?.textContent === expectedListTitle
                && activeView.classList.contains('is-active')
                && activeView.classList.contains('is-transition-destination')
                && row.classList.contains('is-selected')
                && row.classList.contains('is-transition-reveal')
                && row.classList.contains(transitionClass)
                && row.querySelector('.task-row-transition-label')?.textContent.includes('Just moved')
                && document.querySelector('#task-title')?.value === title
                && document.querySelector('#task-status-label')?.textContent === headerLabel
                && document.activeElement === row
                && rowBox.top >= listBox.top - 1
                && rowBox.bottom <= listBox.bottom + 1
            }
            """,
                new { title = taskTitle, view, headerLabel, transitionClass });
        }
        catch (TimeoutException exception)
        {
            var state = await page.EvaluateAsync<string>(
                """
                ({ title, view, headerLabel, transitionClass }) => {
                  const row = [...document.querySelectorAll('#task-list .task-row')]
                    .find(candidate => candidate.querySelector('.task-row-title')?.textContent === title)
                  const list = document.querySelector('#task-list')
                  const activeView = document.querySelector(`.task-view-rail-button[data-task-view="${view}"]`)
                  const expectedListTitle = {
                    active: 'Active',
                    completed: 'Completed',
                    all: 'All statuses'
                  }[view]
                  const rowBox = row?.getBoundingClientRect()
                  const listBox = list?.getBoundingClientRect()
                  return JSON.stringify({
                    listTitle: document.querySelector('#task-list-title')?.textContent,
                    expectedListTitle,
                    activeView: activeView?.className,
                    rowClass: row?.className,
                    rowTransitionLabel: row?.querySelector('.task-row-transition-label')?.textContent,
                    selectedTitle: document.querySelector('#task-title')?.value,
                    statusLabel: document.querySelector('#task-status-label')?.textContent,
                    expectedHeaderLabel: headerLabel,
                    activeElementClass: document.activeElement?.className,
                    rowTop: rowBox?.top,
                    rowBottom: rowBox?.bottom,
                    listTop: listBox?.top,
                    listBottom: listBox?.bottom,
                    transitionClass
                  })
                }
                """,
                new { title = taskTitle, view, headerLabel, transitionClass });
            throw new InvalidOperationException($"Lifecycle destination state did not stabilize: {state}", exception);
        }

        Assert.Equal(
            lifecycleButtonText,
            await page.Locator("#complete-button").TextContentAsync());
        Assert.False(await page.Locator("#complete-button").IsDisabledAsync());
        Assert.Equal(1, await page.Locator("#task-list .task-row.is-selected").CountAsync());
        var actualWorkspaceScrollPosition = await ReadWorkspaceScrollPositionAsync(page);
        Assert.Equal(expectedWorkspaceScrollPosition.Length, actualWorkspaceScrollPosition.Length);
        for (var index = 0; index < expectedWorkspaceScrollPosition.Length; index++)
        {
            Assert.InRange(
                Math.Abs(actualWorkspaceScrollPosition[index] - expectedWorkspaceScrollPosition[index]),
                0,
                1);
        }
        await AssertNoHorizontalPageOverflowAsync(page);
    }

    private static async Task AssertCurrentTaskReadOnlyAsync(
        IPage page,
        string expectedTitle,
        string expectedLifecycleAction = "Reopen")
    {
        await page.WaitForFunctionAsync(
            """
            ({ expectedTitle, expectedLifecycleAction }) => {
              const notice = document.querySelector('#task-read-only-notice')
              return notice?.hidden === false
                && document.querySelector('#task-read-only-title')?.textContent === expectedTitle
                && document.querySelector('#task-title')?.disabled === true
                && document.querySelector('#task-type')?.disabled === true
                && document.querySelector('#task-priority')?.disabled === true
                && document.querySelector('#task-deadline')?.disabled === true
                && document.querySelector('#task-tags')?.disabled === true
                && document.querySelector('#save-button')?.disabled === true
                && document.querySelector('#complete-button')?.disabled === false
                && document.querySelector('#complete-button')?.textContent === expectedLifecycleAction
                && document.querySelector('#task-read-only-reopen-button')?.disabled === false
                && document.querySelector('#checklist-new-text')?.disabled === true
                && document.querySelector('#checklist-add-button')?.disabled === true
                && document.querySelector('#attachment-add-button')?.disabled === true
                && document.querySelector('#comment-text')?.disabled === true
                && document.querySelector('#comment-add-button')?.disabled === true
                && document.querySelector('#editor-host')?.getAttribute('aria-readonly') === 'true'
            }
            """,
            new { expectedTitle, expectedLifecycleAction });

        var editorBody = page.FrameLocator("#editor-host iframe").Locator("body");
        await editorBody.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached
        });
        Assert.Equal("false", await editorBody.GetAttributeAsync("contenteditable"));
    }

    private static Task AssertCurrentTaskEditableAsync(IPage page)
    {
        return page.WaitForFunctionAsync(
            """
            () => document.querySelector('#task-read-only-notice')?.hidden === true
              && document.querySelector('#task-title')?.disabled === false
              && document.querySelector('#task-type')?.disabled === false
              && document.querySelector('#task-priority')?.disabled === false
              && document.querySelector('#task-deadline')?.disabled === false
              && document.querySelector('#task-tags')?.disabled === false
              && document.querySelector('#save-button')?.disabled === false
              && document.querySelector('#checklist-new-text')?.disabled === false
              && document.querySelector('#attachment-add-button')?.disabled === false
              && document.querySelector('#comment-text')?.disabled === false
              && document.querySelector('#editor-host')?.getAttribute('aria-readonly') === 'false'
            """);
    }

    private static Task AssertMarkdownEditorReadOnlyAsync(IPage page)
    {
        return page.WaitForFunctionAsync(
            """
            () => {
              const codeMirror = document.querySelector('#editor-host .CodeMirror')?.CodeMirror
              const wysiwygRoot = document.querySelector('#editor-host [contenteditable][aria-readonly]')
              const toolbarButtons = [...document.querySelectorAll('#editor-host [data-markdown-command]')]
              return codeMirror?.getOption('readOnly') === true
                && wysiwygRoot?.getAttribute('contenteditable') === 'false'
                && wysiwygRoot?.getAttribute('aria-readonly') === 'true'
                && toolbarButtons.length > 0
                && toolbarButtons.every(button => button.disabled)
            }
            """);
    }

    private static Task<double[]> ReadWorkspaceScrollPositionAsync(IPage page)
    {
        return page.EvaluateAsync<double[]>(
            """
            () => {
              const documentScroller = document.scrollingElement
              const editorPanel = document.querySelector('.task-editor-panel')
              const workspaceShell = document.querySelector('.workspace-shell')
              return [
                documentScroller?.scrollLeft ?? window.scrollX,
                documentScroller?.scrollTop ?? window.scrollY,
                editorPanel?.scrollLeft ?? 0,
                editorPanel?.scrollTop ?? 0,
                workspaceShell?.scrollLeft ?? 0,
                workspaceShell?.scrollTop ?? 0
              ]
            }
            """);
    }

    private static async Task CaptureWorkspaceAsync(IPage page, string fileName)
    {
        var captureDirectory = Environment.GetEnvironmentVariable("OKF_TODO_UI_CAPTURE_DIR");
        if (string.IsNullOrWhiteSpace(captureDirectory))
        {
            return;
        }

        Directory.CreateDirectory(captureDirectory);
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(captureDirectory, fileName),
            FullPage = true
        });
    }

    private static async Task CaptureViewportAsync(IPage page, string fileName)
    {
        var captureDirectory = Environment.GetEnvironmentVariable("OKF_TODO_UI_CAPTURE_DIR");
        if (string.IsNullOrWhiteSpace(captureDirectory))
        {
            return;
        }

        Directory.CreateDirectory(captureDirectory);
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(captureDirectory, fileName),
            FullPage = false
        });
    }

    private static Task<bool> IsEditorFocusedAsync(IPage page)
    {
        return page.EvaluateAsync<bool>(
            """
            () => {
              const host = document.querySelector('#editor-host')
              const activeElement = document.activeElement
              return Boolean(host && activeElement && host.contains(activeElement))
            }
            """);
    }

    private sealed class UiAppFixture : IAsyncDisposable
    {
        private readonly WebApplication application;
        private readonly string testDirectory;
        private readonly string databasePath;
        private readonly ConcurrentQueue<string> bridgeMessageTypes;

        private UiAppFixture(
            WebApplication application,
            string testDirectory,
            string databasePath,
            ConcurrentQueue<string> bridgeMessageTypes,
            string baseUrl)
        {
            this.application = application;
            this.testDirectory = testDirectory;
            this.databasePath = databasePath;
            this.bridgeMessageTypes = bridgeMessageTypes;
            BaseUrl = baseUrl;
        }

        public string BaseUrl { get; }

        public string DatabasePath => databasePath;

        public IReadOnlyCollection<string> BridgeMessageTypes => bridgeMessageTypes.ToArray();

        public async Task SendBridgeAsync(string type, object payload)
        {
            var request = JsonSerializer.Serialize(new
            {
                messageId = Guid.NewGuid().ToString("N"),
                type,
                payload
            });
            using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            using var content = new StringContent(request, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("/__ui-test/bridge", content);
            response.EnsureSuccessStatusCode();

            using var responseDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(
                responseDocument.RootElement.GetProperty("ok").GetBoolean(),
                responseDocument.RootElement.ToString());
        }

        public static async Task<UiAppFixture> CreateAsync(bool seedSampleTasks = false)
        {
            var workspaceRoot = FindWorkspaceRoot();
            var testDirectory = Path.Combine(Path.GetTempPath(), "Okf-Todo.UiTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDirectory);
            var databasePath = Path.Combine(testDirectory, "okf-todo-ui-test.db");
            var bridgeMessageTypes = new ConcurrentQueue<string>();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ContentRootPath = workspaceRoot,
                EnvironmentName = "Development"
            });
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(DatabasePathProvider.CreateConnectionString(databasePath, pooling: false)));
            builder.Services.AddSingleton<HtmlSanitizerService>();
            builder.Services.AddSingleton(new HelpRuntimeContextService(workspaceRoot, databasePath));
            builder.Services.AddSingleton<IAppPreferencePathProvider>(
                new TestPreferencePathProvider(Path.Combine(testDirectory, "app-preferences.json")));
            builder.Services.AddSingleton<IBackupDestinationPicker, CancelledBackupDestinationPicker>();
            builder.Services.AddSingleton<ITaskMarkdownExportDestinationPicker>(
                new TestMarkdownExportDestinationPicker(Path.Combine(testDirectory, "task-export.md")));
            builder.Services.AddScoped<LookupSeedService>();
            builder.Services.AddScoped<TaskLifecycleService>();
            builder.Services.AddScoped<TaskListService>();
            builder.Services.AddScoped<TaskService>();
            builder.Services.AddScoped<TaskAttachmentService>();
            builder.Services.AddScoped<TaskChecklistService>();
            builder.Services.AddScoped<TaskRelationService>();
            builder.Services.AddScoped<AppPreferenceService>();
            builder.Services.AddScoped<IssueService>();
            builder.Services.AddScoped<ImageService>();
            builder.Services.AddScoped<DatabaseBackupService>();
            builder.Services.AddScoped<TaskMarkdownExportService>();
            builder.Services.AddScoped<SampleDataSeeder>();
            builder.Services.AddScoped<SampleDataService>();
            builder.Services.AddSingleton<ApplicationCommandService>();
            builder.Services.AddSingleton<BridgeMessageHandler>();

            var application = builder.Build();
            application.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(workspaceRoot, "docs", "help")),
                RequestPath = "/help"
            });
            application.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(workspaceRoot, "Okf-Todo", "wwwroot"))
            });
            application.MapPost("/__ui-test/bridge", async (
                HttpRequest request,
                BridgeMessageHandler handler,
                CancellationToken cancellationToken) =>
            {
                using var reader = new StreamReader(request.Body, Encoding.UTF8);
                var message = await reader.ReadToEndAsync(cancellationToken);
                using var requestDocument = JsonDocument.Parse(message);
                bridgeMessageTypes.Enqueue(requestDocument.RootElement.GetProperty("type").GetString()!);
                var response = await handler.HandleAsync(message, cancellationToken);
                return Results.Text(response, "application/json", Encoding.UTF8);
            });

            await using (var scope = application.Services.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await dbContext.Database.MigrateAsync();
                await scope.ServiceProvider.GetRequiredService<LookupSeedService>().SeedAsync();
                await scope.ServiceProvider.GetRequiredService<TaskListService>().EnsureDefaultListAsync();
                if (seedSampleTasks)
                {
                    await scope.ServiceProvider.GetRequiredService<SampleDataSeeder>().SeedAsync();
                }
            }

            await application.StartAsync();
            var addresses = application.Services
                .GetRequiredService<IServer>()
                .Features
                .Get<IServerAddressesFeature>()
                ?.Addresses
                ?? throw new InvalidOperationException("The UI test server did not expose an address.");
            var baseUrl = addresses.Single(address => address.StartsWith("http://127.0.0.1", StringComparison.Ordinal));

            return new UiAppFixture(application, testDirectory, databasePath, bridgeMessageTypes, baseUrl);
        }

        public async Task<TaskEvidence> ReadTaskEvidenceAsync(string title)
        {
            await using var connection = new SqliteConnection(
                $"Data Source={databasePath};Mode=ReadOnly;Pooling=False");
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    task.Id,
                    (SELECT COUNT(*) FROM TaskChecklistItems checklist WHERE checklist.TaskId = task.Id),
                    (SELECT COUNT(*) FROM TaskAttachments attachment WHERE attachment.TaskId = task.Id),
                    (SELECT COUNT(*) FROM TaskComments comment WHERE comment.TaskId = task.Id),
                    (SELECT COUNT(*) FROM TaskLogEntries log WHERE log.TaskId = task.Id)
                FROM TaskItems task
                WHERE task.Title = $title;
                """;
            command.Parameters.AddWithValue("$title", title);

            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync(), "The task was not persisted in the isolated SQLite database.");
            return new TaskEvidence(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetInt32(2),
                reader.GetInt32(3),
                reader.GetInt32(4));
        }

        public async Task<string> ReadTaskBodyAsync(string title)
        {
            await using var connection = new SqliteConnection(
                $"Data Source={databasePath};Mode=ReadOnly;Pooling=False");
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT Body
                FROM TaskItems
                WHERE Title = $title;
                """;
            command.Parameters.AddWithValue("$title", title);

            var body = await command.ExecuteScalarAsync();
            Assert.NotNull(body);
            return Convert.ToString(body) ?? string.Empty;
        }

        public async Task<string> ReadTaskExportAsync()
        {
            var exportPath = Path.Combine(testDirectory, "task-export.md");
            Assert.True(File.Exists(exportPath), "The Markdown export file was not created.");
            return await File.ReadAllTextAsync(exportPath);
        }

        public async ValueTask DisposeAsync()
        {
            await application.StopAsync();
            await application.DisposeAsync();

            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }

        private static string FindWorkspaceRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Okf-Todo.slnx")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the OKF-Todo workspace root.");
        }
    }

    private sealed class TestPreferencePathProvider(string path) : IAppPreferencePathProvider
    {
        public string GetPreferencesPath() => path;
    }

    private sealed class CancelledBackupDestinationPicker : IBackupDestinationPicker
    {
        public Task<string?> PickAsync(
            string suggestedFileName,
            string? initialDirectory,
            CancellationToken cancellationToken) => Task.FromResult<string?>(null);
    }

    private sealed class TestMarkdownExportDestinationPicker(string exportPath)
        : ITaskMarkdownExportDestinationPicker
    {
        public Task<string?> PickAsync(
            string suggestedFileName,
            string? initialDirectory,
            CancellationToken cancellationToken) => Task.FromResult<string?>(exportPath);
    }

    private sealed record TaskEvidence(
        int TaskId,
        int ChecklistItemCount,
        int AttachmentCount,
        int CommentCount,
        int LogCount);
}
