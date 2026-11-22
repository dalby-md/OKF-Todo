using Microsoft.Playwright;

namespace Okf_Todo.UiTests;

// Helpers shared by the focused workspace UI tests.
public sealed partial class NewTaskDialogUiTests
{
    /// <summary>
    /// Starts a fresh app and browser, opens the workspace and waits until lookups are loaded.
    /// Each test gets its own browser: a long-running shared Edge process made keyboard focus
    /// checks unreliable after many tests.
    /// <paramref name="prepare"/> runs against the app before the page opens, for state the
    /// test needs but does not exercise. With <paramref name="failOnConsoleErrors"/>, any
    /// console error logged by the page fails the test.
    /// </summary>
    private static async Task RunWorkspaceTestAsync(
        string pageVersion,
        Func<IPage, UiAppFixture, Task> test,
        bool seedSampleTasks = false,
        int width = 1487,
        int height = 1058,
        Func<UiAppFixture, Task>? prepare = null,
        bool failOnConsoleErrors = false)
    {
        await using var fixture = await UiAppFixture.CreateAsync(seedSampleTasks);
        if (prepare is not null)
        {
            await prepare(fixture);
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height }
        });
        await context.AddInitScriptAsync(BridgeAdapterScript);

        var page = await context.NewPageAsync();
        var consoleErrors = new List<string>();
        page.Console += (_, message) =>
        {
            if (message.Type == "error")
            {
                lock (consoleErrors)
                {
                    consoleErrors.Add(message.Text);
                }
            }
        };
        await page.GotoAsync(
            $"{fixture.BaseUrl}/index.html?v={pageVersion}",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-type option').length > 0");
        await test(page, fixture);

        if (failOnConsoleErrors)
        {
            lock (consoleErrors)
            {
                Assert.Empty(consoleErrors);
            }
        }
    }

    /// <summary>
    /// Creates a task through the New task dialog and waits until creation has finished.
    /// The dialog closes before the app reloads the task list, so the dialog alone is not enough.
    /// </summary>
    private static async Task CreateTaskThroughDialogAsync(IPage page, string title)
    {
        await page.Locator("#new-task-button").ClickAsync();
        await page.Locator("#new-task-title-input").FillAsync(title);
        await page.Locator("#new-task-save-button").ClickAsync();
        await page.Locator("#new-task-overlay").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden
        });
        await page.WaitForFunctionAsync("() => document.querySelector('#save-status')?.textContent === 'Saved'");
    }

    /// <summary>
    /// Opens a task view from the rail and waits until its list has finished loading.
    /// The heading changes immediately; the list itself is busy until it has been reloaded.
    /// </summary>
    private static async Task OpenTaskViewAsync(IPage page, string view, string heading, bool dispatchClick = false)
    {
        var button = page.Locator($".task-view-rail-button[data-task-view='{view}']");
        if (dispatchClick)
        {
            await button.DispatchEventAsync("click");
        }
        else
        {
            await button.ClickAsync();
        }

        await page.WaitForFunctionAsync(
            "heading => document.querySelector('#task-list-title')?.textContent === heading && !document.querySelector('#task-list').hasAttribute('aria-busy')",
            heading);
    }

    private static ILocator TaskRowShell(IPage page, string title) =>
        page.Locator(".task-row-shell").Filter(new LocatorFilterOptions { HasText = title });
}
