using Microsoft.Playwright;

namespace Okf_Todo.UiTests;

// Compiled only with -p:ScreenshotCapture=true. Uses the existing isolated
// UI fixture, real services and sample data; never opens the user's database.
public sealed partial class NewTaskDialogUiTests
{
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
