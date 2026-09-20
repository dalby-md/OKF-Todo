using Microsoft.Playwright;

namespace Okf_Todo.UiTests;

// Compiled only with -p:ScreenshotCapture=true. Uses the existing isolated
// UI fixture, real services and sample data; never opens the user's database.
public sealed partial class NewTaskDialogUiTests
{
    [Fact]
    public async Task CaptureMarkdownEditorScreenshot()
    {
        var output = Environment.GetEnvironmentVariable("OKF_SCREENSHOT_OUTPUT")
            ?? throw new InvalidOperationException("Set OKF_SCREENSHOT_OUTPUT to the screenshot directory.");
        Directory.CreateDirectory(output);
        await using var fixture = await UiAppFixture.CreateAsync(seedSampleTasks: true);
        await fixture.SendBridgeAsync("editor.preference.save", new
        {
            bodyFormatCode = "MARKDOWN", markdownEditType = "MARKDOWN", editorHeight = 620
        });
        const string title = "Investigate API timeouts";
        await fixture.SendBridgeAsync("task.create", new
        {
            title, taskTypeCode = "INVESTIGATION", bodyFormatCode = "MARKDOWN",
            body = """
                ## Investigation notes

                **Symptom:** Some requests time out after 30 seconds.

                ### What we know
                - The API is healthy after a restart.
                - Failures increase during the nightly import.
                - Check `connection_timeout` before retrying.

                ### Reproduce
                ```http
                GET /api/tasks?status=active
                X-Correlation-ID: support-042
                ```

                > Capture the correlation ID with each failed request.

                ### Next step
                Compare the database trace with the application log.
                """
        });
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Channel = "msedge", Headless = true, IgnoreDefaultArgs = ["--hide-scrollbars"]
        });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1600, Height = 900 },
            DeviceScaleFactor = 1.2f, Locale = "en-GB", ColorScheme = ColorScheme.Light,
            ReducedMotion = ReducedMotion.Reduce
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{fixture.BaseUrl}/index.html");
        await page.WaitForFunctionAsync("() => document.querySelector('#save-status')?.textContent === 'Loaded'");
        await page.Locator("#task-selection-coachmark-dismiss").ClickAsync();
        await page.Locator("#task-list .task-row").Filter(new() { HasText = title }).ClickAsync();
        await page.Locator("#editor-host .tui-editor-defaultUI").WaitForAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.Locator(".task-editor-panel").EvaluateAsync("panel => { const editor = document.querySelector('#editor-host'); panel.scrollTop += editor.getBoundingClientRect().top - panel.getBoundingClientRect().top - 30; }");
        await page.Mouse.MoveAsync(1598, 898);
        await page.EvaluateAsync("document.fonts.ready");
        await AssertNoHorizontalPageOverflowAsync(page);
        Assert.Equal(0, await page.Locator(".app-topbar").EvaluateAsync<double>("el => el.getBoundingClientRect().top"));
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(output, "07-write-with-markdown.png"), Animations = ScreenshotAnimations.Disabled
        });
    }

    [Fact]
    public async Task CaptureProductScreenshots()
    {
        var output = Environment.GetEnvironmentVariable("OKF_SCREENSHOT_OUTPUT")
            ?? throw new InvalidOperationException("Set OKF_SCREENSHOT_OUTPUT to the screenshot directory.");
        Directory.CreateDirectory(output);
        await using var fixture = await UiAppFixture.CreateAsync(seedSampleTasks: true);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Channel = "msedge", Headless = true
        });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1600, Height = 900 },
            DeviceScaleFactor = 1.2f, Locale = "en-GB", ColorScheme = ColorScheme.Light
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{fixture.BaseUrl}/index.html");
        await page.Locator("#task-list .task-row").First.WaitForAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.Locator("#task-selection-coachmark-dismiss").ClickAsync();

        async Task OpenTask(string title)
        {
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(500);
            if (await page.Locator("#task-title").InputValueAsync() != title)
                await page.Locator("#task-list .task-row").Filter(new() { HasText = title }).ClickAsync();
            await page.WaitForFunctionAsync("title => document.querySelector('#task-title')?.value === title", title);
            await page.Locator("#editor-host .tox-tinymce, #editor-host .toastui-editor-defaultUI").First.WaitForAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.Locator("#checklist-list input[type='checkbox']").First.WaitForAsync();
        }
        async Task Capture(string name)
        {
            await page.Mouse.MoveAsync(1598, 898);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.EvaluateAsync("document.fonts.ready");
            await page.WaitForTimeoutAsync(700);
            await AssertNoHorizontalPageOverflowAsync(page);
            await page.ScreenshotAsync(new() { Path = Path.Combine(output, name), Animations = ScreenshotAnimations.Disabled });
        }
        async Task View(string view)
        {
            await page.Locator($".task-view-rail-button[data-task-view='{view}']").ClickAsync();
            await page.WaitForFunctionAsync("view => document.querySelector('#task-view').value === view", view);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        await OpenTask("Fix failed production deployment");
        await Capture("01-task-workspace.png");
        await View("actnow");
        await OpenTask("Review urgent security patch");
        await Capture("02-focus-on-what-matters.png");
        await View("waiting");
        await OpenTask("Investigate intermittent API timeout");
        await Capture("03-track-follow-ups.png");
        await View("active");
        await OpenTask("Fix failed production deployment");
        await page.Locator("#checklist-title").EvaluateAsync("el => el.scrollIntoView({block:'start'})");
        await page.EvaluateAsync("window.scrollTo(0, 0)");
        await Capture("04-keep-the-details-together.png");
        await page.Locator("#task-title").ScrollIntoViewIfNeededAsync();
        await page.Locator("#settings-button").ClickAsync();
        await page.Locator("[data-preference-select='color-scheme'] [data-value='DARK']").ClickAsync();
        await page.Locator("#settings-close-button").ClickAsync();
        await Capture("05-work-in-dark-mode.png");
        await page.Locator("#settings-button").ClickAsync();
        await page.Locator("[data-preference-select='color-scheme'] [data-value='LIGHT']").ClickAsync();
        await page.Locator("[data-preference-section='database']").ClickAsync();
        await Capture("06-protect-your-local-work.png");
    }
}
