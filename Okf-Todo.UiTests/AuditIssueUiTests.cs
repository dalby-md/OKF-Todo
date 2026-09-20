using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Playwright;

namespace Okf_Todo.UiTests;

public sealed partial class NewTaskDialogUiTests
{
    [Theory]
    [InlineData(1909, 1083, 20)]
    [InlineData(1280, 720, 16)]
    [InlineData(1280, 720, 20)]
    [InlineData(820, 720, 20)]
    [InlineData(1273, 722, 16)]
    public async Task TaskDetails_MouseWheelReachesCommentsWithinTheWindow(int width, int height, int fontSize)
    {
        await RunAuditCheckAsync(async (page, fixture) =>
        {
            await page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
            await page.SetViewportSizeAsync(width, height);
            if (fontSize == 16)
            {
                await fixture.SendBridgeAsync("editor.preference.save", new { bodyFormatCode = "MARKDOWN", markdownEditType = "MARKDOWN", editorHeight = 360 });
                await page.ReloadAsync();
                await page.WaitForFunctionAsync("() => document.querySelector('#save-status')?.textContent === 'Loaded'");
            }
            await page.EvaluateAsync("size => document.documentElement.style.setProperty('--app-font-size', `${size}px`)", fontSize);
            await page.Locator("#new-task-button").ClickAsync();
            await page.Locator("#new-task-title-input").FillAsync("CHECK — scrolling to comments");
            await page.Locator("#new-task-save-button").ClickAsync();
            await page.Locator("#new-task-overlay").WaitForAsync(new() { State = WaitForSelectorState.Hidden });
            foreach (var item in new[] { "One", "Two" })
            {
                await page.Locator("#checklist-new-text").FillAsync(item);
                await page.Locator("#checklist-add-button").ClickAsync();
                await page.WaitForFunctionAsync("text => [...document.querySelectorAll('.checklist-text')].some(el => el.value === text)", item);
            }
            const string comment = "This saved comment must be reachable by scrolling.";
            await page.Locator("#comment-text").FillAsync(comment);
            await page.Locator("#comment-add-button").ClickAsync();
            await page.Locator(".timeline-entry-comment").Filter(new() { HasText = comment }).WaitForAsync();
            await page.WaitForFunctionAsync("() => document.querySelector('#save-status').textContent === 'Comment added'");

            var panel = page.Locator(".task-editor-panel");
            var bounds = await panel.BoundingBoxAsync();
            Assert.NotNull(bounds);
            Assert.True(bounds.Y + bounds.Height <= height + 1, $"Task details extend below the {height}px viewport: {bounds.Y + bounds.Height}px.");
            Assert.True(bounds.X + bounds.Width <= width + 1, "The detail scrollbar must stay inside the window.");
            await panel.EvaluateAsync("el => { el.scrollTop = 0; }");
            var queueScroll = await page.Locator("#task-list").EvaluateAsync<double>("el => el.scrollTop");
            await page.Mouse.MoveAsync(bounds.X + bounds.Width - 30, bounds.Y + bounds.Height / 2);
            await page.Mouse.WheelAsync(0, 10000);
            await page.WaitForFunctionAsync("() => { const el = document.querySelector('.task-editor-panel'); return el.scrollTop > 0 && el.scrollTop + el.clientHeight >= el.scrollHeight - 2; }");
            var composer = await page.Locator("#comment-form").BoundingBoxAsync();
            Assert.NotNull(composer);
            await CaptureViewportAsync(page, $"task-details-scroll-{width}-{height}-{fontSize}.png");
            var scrollMetrics = await panel.EvaluateAsync<string>("el => JSON.stringify({clientHeight:el.clientHeight, scrollHeight:el.scrollHeight, scrollTop:el.scrollTop, form:document.querySelector('#task-form').getBoundingClientRect().toJSON(), composer:document.querySelector('#comment-form').getBoundingClientRect().toJSON()})");
            Assert.True(composer.Y >= bounds.Y && composer.Y + composer.Height <= height - 12, $"The complete comment composer must be reachable with space below it. {scrollMetrics}");
            Assert.True(await panel.EvaluateAsync<bool>("el => el.offsetWidth - el.clientWidth >= 10"), "A usable scrollbar must remain visible at the right edge of task details.");
            Assert.Equal(queueScroll, await page.Locator("#task-list").EvaluateAsync<double>("el => el.scrollTop"));
            Assert.Equal(0, await page.EvaluateAsync<double>("window.scrollY"));

            // Exercise the visible scrollbar without Playwright scrolling a control into view.
            await panel.EvaluateAsync("el => { el.scrollTop = 0; }");
            var thumbHeight = await panel.EvaluateAsync<double>("el => Math.max(36, el.clientHeight * el.clientHeight / el.scrollHeight)");
            await page.Mouse.MoveAsync(bounds.X + bounds.Width - 7, bounds.Y + (float)thumbHeight / 2);
            await page.Mouse.DownAsync();
            await page.Mouse.MoveAsync(bounds.X + bounds.Width - 7, bounds.Y + bounds.Height - 2, new() { Steps = 8 });
            await page.Mouse.UpAsync();
            await page.WaitForFunctionAsync("() => { const el = document.querySelector('.task-editor-panel'); return el.scrollTop + el.clientHeight >= el.scrollHeight - 2; }");

            await page.EvaluateAsync("() => { document.documentElement.classList.add('theme-dark'); document.getElementById('dark-theme-stylesheet').disabled = false; }");
            Assert.True(await panel.EvaluateAsync<bool>("el => el.offsetWidth - el.clientWidth >= 10"));
            await CaptureViewportAsync(page, $"task-details-scroll-dark-{width}-{height}-{fontSize}.png");
        });
    }

    [Fact]
    public async Task Audit_ModalFocusStaysInTopDialogAndReturnsToOpener()
    {
        await RunAuditCheckAsync(async (page, fixture) =>
        {
            await page.Locator("#settings-button").ClickAsync();
            await page.Locator("[data-preference-section='data-values']").ClickAsync();
            await page.Locator("#lookup-reset-all-colors-button").ClickAsync();
            await page.WaitForFunctionAsync("() => document.activeElement.id === 'confirmation-cancel-button'");
            await page.Keyboard.PressAsync("Shift+Tab");
            Assert.Equal("confirmation-confirm-button", await page.EvaluateAsync<string>("document.activeElement.id"));
            await page.Keyboard.PressAsync("Tab");
            Assert.Equal("confirmation-cancel-button", await page.EvaluateAsync<string>("document.activeElement.id"));
            await page.Locator("#settings-close-button").EvaluateAsync("button => button.focus()");
            Assert.True(await page.EvaluateAsync<bool>("document.activeElement.closest('#confirmation-overlay') !== null"));
            await page.Keyboard.PressAsync("Escape");
            await page.WaitForFunctionAsync("() => document.activeElement.id === 'lookup-reset-all-colors-button'");
            Assert.True(await page.Locator("#settings-overlay").IsVisibleAsync());
            Assert.DoesNotContain("lookup.settings.resetAllColors", fixture.BridgeMessageTypes);
            await page.Keyboard.PressAsync("Escape");
            await page.WaitForFunctionAsync("() => document.activeElement.id === 'settings-button'");
            await page.Locator("#new-task-button").ClickAsync();
            Assert.Equal("Create task", await page.Locator("#new-task-save-button").TextContentAsync());
            await page.Keyboard.PressAsync("Escape");
            Assert.Equal("Cancel task", await page.Locator("#cancel-button").TextContentAsync());
        });
    }

    [Fact]
    public async Task Audit_LongTagsWrapAndRemainEditableAtLaptopAndLargeTextSizes()
    {
        await RunAuditCheckAsync(async (page, _) =>
        {
            await page.Locator("#task-search").FillAsync("Follow up on ServiceDesk incident");
            await page.Locator("#task-list .task-row").First.ClickAsync();
            await page.WaitForFunctionAsync("() => document.querySelector('#task-title').value.includes('Follow up on ServiceDesk incident')");
            var longTag = "support-investigation-with-a-very-long-customer-reference-without-spaces";
            await page.Locator(".tags-field .select2-search__field").FillAsync(longTag);
            await page.Locator(".tags-field .select2-search__field").PressAsync("Enter");
            await page.Locator("#save-button").ClickAsync();
            await page.WaitForFunctionAsync("() => document.querySelector('#save-status').textContent === 'Saved'");
            foreach (var width in new[] { 1280, 1600 })
            {
                await page.SetViewportSizeAsync(width, 900);
                foreach (var fontSize in new[] { "16px", "20px" })
                {
                    await page.EvaluateAsync("size => document.documentElement.style.setProperty('--app-font-size', size)", fontSize);
                    Assert.True(await page.Locator(".task-editor-panel").EvaluateAsync<bool>("el => el.scrollWidth <= el.clientWidth + 1"));
                    Assert.True(await page.Locator(".tags-field").EvaluateAsync<bool>("el => el.scrollWidth <= el.clientWidth + 1"));
                }
            }
            var chip = page.Locator(".tags-field .select2-selection__choice").Filter(new() { HasText = longTag });
            Assert.True(await chip.IsVisibleAsync());
            await chip.Locator(".select2-selection__choice__remove").ClickAsync();
            Assert.Equal(0, await page.Locator(".tags-field .select2-selection__choice").Filter(new() { HasText = longTag }).CountAsync());
            await CaptureViewportAsync(page, "audit-tags-large-text.png");
        });
    }

    [Fact]
    public async Task Audit_TimelineFiltersPreserveHistoryAndRevealHiddenContext()
    {
        await RunAuditCheckAsync(async (page, fixture) =>
        {
            await page.Locator("#task-search").FillAsync("Follow up on ServiceDesk incident");
            await page.Locator("#task-list .task-row").First.ClickAsync();
            await page.Locator("#task-hidden-context-toggle").WaitForAsync();
            Assert.True(await page.Locator(".source-grid").IsHiddenAsync());
            await page.Locator("#task-hidden-context-toggle").ClickAsync();
            Assert.True(await page.Locator(".source-grid").IsVisibleAsync());
            Assert.False(await page.Locator("#show-source-fields").IsCheckedAsync());
            await page.Locator("#jump-to-comment").ClickAsync();
            Assert.Equal("comment-text", await page.EvaluateAsync<string>("document.activeElement.id"));
            const string comment = "Audit regression: customer supplied the missing logs.";
            await page.Locator("#comment-text").FillAsync(comment);
            await page.Locator("#comment-add-button").ClickAsync();
            await page.Locator(".timeline-entry-comment").Filter(new() { HasText = comment }).WaitForAsync();
            await page.Locator("#timeline-filter").SelectOptionAsync("comments");
            Assert.Equal(0, await page.Locator(".timeline-entry-log").CountAsync());
            Assert.True(await page.Locator(".timeline-entry-comment").CountAsync() > 0);
            await page.Locator("#timeline-filter").SelectOptionAsync("changes");
            Assert.Equal(0, await page.Locator(".timeline-entry-comment").CountAsync());
            Assert.DoesNotContain("Comment added", await page.Locator("#timeline-list").TextContentAsync());
            var evidence = await fixture.ReadTaskEvidenceAsync("Follow up on ServiceDesk incident");
            var response = await page.APIRequest.PostAsync($"{fixture.BaseUrl}/__ui-test/bridge", new()
            {
                DataObject = new { messageId = "audit-history", type = "task.timeline.get", payload = new { taskId = evidence.TaskId } }
            });
            Assert.Contains("COMMENT_ADDED", await response.TextAsync());
            await page.Locator("#cancel-button").ClickAsync();
            await page.WaitForFunctionAsync("() => document.querySelector('#task-form').classList.contains('is-task-read-only')");
            Assert.False(await page.Locator("#timeline-filter").IsDisabledAsync());
            await page.Locator("#timeline-filter").SelectOptionAsync("comments");
            Assert.True(await page.Locator(".timeline-entry-comment").Filter(new() { HasText = comment }).IsVisibleAsync());
            Assert.True(await page.Locator("#jump-to-comment").IsHiddenAsync());
            await CaptureViewportAsync(page, "audit-timeline.png");
        });
    }

    [Fact]
    public async Task Audit_EmptyTrashAndHelpSectionLinksHaveUsefulKeyboardNavigation()
    {
        await RunAuditCheckAsync(async (page, _) =>
        {
            await page.Locator("[data-task-view='trash']").ClickAsync();
            await page.Locator("#task-list .empty-list").WaitForAsync();
            Assert.Contains("Trash is empty", await page.Locator("#task-list").TextContentAsync());
            Assert.DoesNotContain("Create a task", await page.Locator("#task-list").TextContentAsync());
            Assert.True(await page.Locator("#task-form").IsHiddenAsync());
            Assert.True(await page.Locator("#task-empty-details").IsVisibleAsync());
            await page.Locator("#help-button").ClickAsync();
            var link = page.Locator("#help-content a[href='#timeline-and-comments']");
            await link.WaitForAsync();
            Assert.True(await page.Locator("#help-content").EvaluateAsync<bool>("el => [...el.querySelectorAll('a[href^=\"#\"]:not([data-help-topic-link])')].every(a => document.getElementById(a.getAttribute('href').slice(1)))"));
            await link.FocusAsync();
            await page.Keyboard.PressAsync("Enter");
            Assert.Equal("timeline-and-comments", await page.EvaluateAsync<string>("document.activeElement.id"));
            await CaptureViewportAsync(page, "audit-help-navigation.png");
        });
    }

    [Fact]
    public async Task Audit_ExportPresetsProduceMarkdownAndHtmlWithoutExpandingContentScope()
    {
        await RunAuditCheckAsync(async (page, fixture) =>
        {
            await page.Locator("#task-export-button").ClickAsync();
            await page.Locator("#task-export-preset").SelectOptionAsync("brief");
            Assert.True(await page.Locator("#task-export-composer").IsHiddenAsync());
            Assert.Contains("Title, Status, Deadline", await page.Locator("#task-export-included-fields").TextContentAsync());
            Assert.DoesNotContain("List,", await page.Locator("#task-export-included-fields").TextContentAsync());
            await CaptureViewportAsync(page, "audit-export-presets.png");
            await page.Locator("#task-export-confirm-button").ClickAsync();
            await page.Locator("#task-export-overlay").WaitForAsync(new() { State = WaitForSelectorState.Hidden });
            var markdown = await fixture.ReadTaskExportAsync();
            Assert.Contains("| Title | Status | Deadline |", markdown);
            Assert.DoesNotContain("| Body |", markdown);
            Assert.DoesNotContain("| Comments |", markdown);
            await page.Locator("#task-export-button").ClickAsync();
            await page.Locator("#task-export-preset").SelectOptionAsync("support");
            await page.Locator("#task-export-copy-html-button").ClickAsync();
            await page.Locator("#task-export-overlay").WaitForAsync(new() { State = WaitForSelectorState.Hidden });
            using var clipboard = JsonDocument.Parse(await page.EvaluateAsync<string>("JSON.stringify(window.__clipboardWrite)"));
            var html = clipboard.RootElement.GetProperty("html").GetString()!;
            Assert.Contains(">Waiting for</th>", html);
            Assert.Contains(">Owner</th>", html);
            Assert.DoesNotContain(">Body</th>", html);
            Assert.DoesNotContain(">Comments</th>", html);
        });
    }

    private static async Task RunAuditCheckAsync(Func<IPage, UiAppFixture, Task> check)
    {
        await using var fixture = await UiAppFixture.CreateAsync(seedSampleTasks: true);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Channel = "msedge", Headless = true, IgnoreDefaultArgs = ["--hide-scrollbars"] });
        await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1280, Height = 720 } });
        await context.AddInitScriptAsync(BridgeAdapterScript);
        await context.AddInitScriptAsync(ClipboardAdapterScript);
        var page = await context.NewPageAsync();
        var errors = new ConcurrentQueue<string>();
        page.PageError += (_, error) => errors.Enqueue(error);
        await page.GotoAsync($"{fixture.BaseUrl}/index.html", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync("() => document.querySelector('#save-status')?.textContent === 'Loaded'");
        if (await page.Locator("#task-selection-coachmark-dismiss").IsVisibleAsync())
            await page.Locator("#task-selection-coachmark-dismiss").ClickAsync();
        await check(page, fixture);
        Assert.Empty(errors);
    }
}
