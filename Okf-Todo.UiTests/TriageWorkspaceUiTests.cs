using System.Text.Json;
using Microsoft.Playwright;

namespace Okf_Todo.UiTests;

// The triage workspace on the sample data: sorting, the views in the rail, the waiting
// field, and how the layout adapts to window size and layout preferences.
public sealed partial class NewTaskDialogUiTests
{
    [Fact]
    public async Task TaskSort_OffersTriageAndStatusOrderWithMatchingHelp()
    {
        await RunWorkspaceTestAsync("triage-sort", seedSampleTasks: true, test: async (page, fixture) =>
        {
            Assert.Equal("Triage order", await page.Locator("#task-sort option[value='ATTENTION']").TextContentAsync());
            Assert.Equal("Status order", await page.Locator("#task-sort option[value='STATUS']").TextContentAsync());
            Assert.Contains("overdue, urgent, ready active work", await page.Locator("#task-sort-help").GetAttributeAsync("title"));
            await page.Locator("#task-sort").SelectOptionAsync("STATUS");
            Assert.Contains("Mainly useful in All", await page.Locator("#task-sort-help").GetAttributeAsync("title"));
        });
    }

    [Fact]
    public async Task LargeWindow_ShowsRailListAndEditorSideBySide()
    {
        await RunWorkspaceTestAsync("triage-large", seedSampleTasks: true, test: async (page, fixture) =>
        {
            await WaitForFirstTaskLoadedAsync(page);
            var rail = await page.Locator(".task-view-rail").BoundingBoxAsync();
            var list = await page.Locator(".task-sidebar").BoundingBoxAsync();
            var editor = await page.Locator(".task-editor-panel").BoundingBoxAsync();
            Assert.NotNull(rail);
            Assert.NotNull(list);
            Assert.NotNull(editor);
            Assert.True(rail.Width >= 160);
            Assert.True(list.Width >= 360);
            Assert.True(rail.X < list.X);
            Assert.True(list.X < editor.X);
            Assert.False(await page.Locator(".task-view-rail-label").First.IsHiddenAsync());
            Assert.True(await page.Locator(".task-view-compact").IsHiddenAsync());
            var semanticIconColors = await page.Locator(".task-view-rail-button .fluent-icon")
                .EvaluateAllAsync<string[]>("icons => icons.map(icon => getComputedStyle(icon).color)");
            Assert.True(
                semanticIconColors.Distinct(StringComparer.Ordinal).Count() >= 5,
                "Expected the task views to retain distinct semantic icon colors.");
            await AssertNoHorizontalPageOverflowAsync(page);

            await OpenTaskViewAsync(page, "active", "Active");
            await CaptureWorkspaceAsync(page, "triage-command-large.png");
        });
    }

    [Fact]
    public async Task ActNowView_ListsOnlyAttentionTasksThatAreNotWaiting()
    {
        await RunWorkspaceTestAsync("triage-act-now", seedSampleTasks: true, test: async (page, fixture) =>
        {
            await OpenTaskViewAsync(page, "attention", "Attention");
            var attentionCount = await page.Locator("#task-list .task-row").CountAsync();

            await OpenTaskViewAsync(page, "actnow", "Act now");
            Assert.Equal("actnow", await page.Locator("#task-view").InputValueAsync());
            Assert.Contains(
                "not waiting",
                await page.Locator(".task-view-rail-button[data-task-view='actnow']").GetAttributeAsync("title"));
            var actNowCount = await page.Locator("#task-list .task-row").CountAsync();
            Assert.True(actNowCount > 0);
            Assert.True(actNowCount <= attentionCount);
            Assert.Equal(0, await page.Locator("#task-list .task-badge-waiting").CountAsync());
        });
    }

    [Fact]
    public async Task WaitingField_ClearsInPlaceAndSavingMovesTheTaskBetweenWaitingAndReady()
    {
        await RunWorkspaceTestAsync("triage-waiting", seedSampleTasks: true, test: async (page, fixture) =>
        {
            await OpenTaskViewAsync(page, "actnow", "Act now");
            var taskId = await page.Locator("#task-list .task-row").First.GetAttributeAsync("data-task-id");
            var taskTitle = await page.Locator("#task-list .task-row-title").First.TextContentAsync();
            await page.Locator("#task-list .task-row").First.ClickAsync();
            // The title is filled in before the rest of the task loads. The final load step
            // re-renders the waiting field, so wait until the form is no longer busy.
            await page.WaitForFunctionAsync(
                "title => { const form = document.querySelector('#task-form'); return document.querySelector('#task-title')?.value === title && form && !form.hasAttribute('aria-busy'); }",
                taskTitle);

            Assert.True(await page.Locator("#waiting-clear-button").IsHiddenAsync());
            await page.Locator("#waiting-text").FillAsync("Temporary wait");
            Assert.True(await page.Locator("#waiting-clear-button").IsVisibleAsync());
            await page.Locator("#waiting-clear-button").ClickAsync();
            Assert.Equal("", await page.Locator("#waiting-text").InputValueAsync());
            Assert.True(await page.Locator("#waiting-clear-button").IsHiddenAsync());
            await page.WaitForFunctionAsync("() => document.activeElement?.id === 'waiting-text'");

            await page.Locator("#waiting-text").FillAsync("External response");
            await page.Locator("#save-button").ClickAsync();
            await page.WaitForFunctionAsync("() => document.querySelector('#save-status')?.textContent === 'Saved'");
            await page.WaitForFunctionAsync("() => document.querySelector('#task-list-title').textContent === 'Waiting'");

            Assert.True(await page.Locator("#waiting-clear-button").IsVisibleAsync());
            await page.Locator("#waiting-clear-button").ClickAsync();
            Assert.Equal("Unsaved changes", await page.Locator("#save-status").TextContentAsync());
            Assert.Equal("Waiting", await page.Locator("#task-list-title").TextContentAsync());
            await page.Locator("#save-button").ClickAsync();
            await page.WaitForFunctionAsync("() => document.querySelector('#task-list-title').textContent === 'Ready'");
            Assert.Equal("", await page.Locator("#waiting-text").InputValueAsync());
            Assert.True(await page.Locator("#waiting-clear-button").IsHiddenAsync());
            Assert.Equal(taskId, await page.Locator(".task-row[aria-current='true']").GetAttributeAsync("data-task-id"));
        });
    }

    [Fact]
    public async Task StatusMessage_LongTextStaysReadableWithoutOverflow()
    {
        await RunWorkspaceTestAsync("triage-status-message", seedSampleTasks: true, width: 1280, height: 900, test: async (page, fixture) =>
        {
            await WaitForFirstTaskLoadedAsync(page);
            const string longStatusMessage = "MCP configuration copied for the current source checkout";
            await page.Locator("#save-status").EvaluateAsync(
                "(element, message) => { element.textContent = message; element.className = 'save-status is-saved'; }",
                longStatusMessage);
            Assert.Equal(longStatusMessage, await page.Locator("#save-status").TextContentAsync());
            var statusLayout = await page.Locator("#save-status").EvaluateAsync<JsonElement>(
                """
                element => ({
                  clientWidth: element.clientWidth,
                  scrollWidth: element.scrollWidth,
                  whiteSpace: getComputedStyle(element).whiteSpace,
                  textOverflow: getComputedStyle(element).textOverflow
                })
                """);
            Assert.True(statusLayout.GetProperty("clientWidth").GetInt32() > 132);
            Assert.True(statusLayout.GetProperty("scrollWidth").GetInt32() <= statusLayout.GetProperty("clientWidth").GetInt32());
            Assert.Equal("normal", statusLayout.GetProperty("whiteSpace").GetString());
            Assert.Equal("clip", statusLayout.GetProperty("textOverflow").GetString());
            await AssertNoHorizontalPageOverflowAsync(page);
            await CaptureWorkspaceAsync(page, "status-message-readable.png");
        });
    }

    [Fact]
    public async Task NarrowerWindows_UseACompactRailAndThenAStackedLayout()
    {
        await RunWorkspaceTestAsync("triage-narrow", seedSampleTasks: true, test: async (page, fixture) =>
        {
            await OpenTaskViewAsync(page, "active", "Active");
            await WaitForFirstTaskLoadedAsync(page);

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
        });
    }

    [Fact]
    public async Task TaskFilterLayout_ExpandedPreferenceShowsFiltersInline()
    {
        await RunWorkspaceTestAsync("triage-filter-layout", seedSampleTasks: true, test: async (page, fixture) =>
        {
            await OpenAppearancePreferencesAsync(page);
            Assert.Equal(
                "appearance",
                await page.Locator(".preferences-layout-control")
                    .EvaluateAsync<string>("control => control.closest('[data-preference-panel]').dataset.preferencePanel"));
            Assert.True(await page.Locator("[data-preference-select='task-filter-layout'] .preference-choice[data-value='COMPACT']")
                .EvaluateAsync<bool>("button => button.classList.contains('is-selected')"));
            await page.Locator("[data-preference-select='task-filter-layout'] .preference-choice[data-value='EXPANDED']").ClickAsync();
            await page.WaitForFunctionAsync(
                "() => document.documentElement.classList.contains('task-filter-layout-expanded') && document.querySelector('#save-status').textContent === 'Task filter layout saved'");
            await page.Locator("#settings-close-button").ClickAsync();

            Assert.True(await page.Locator("#task-filter-button").IsHiddenAsync());
            Assert.False(await page.Locator("#task-filter-popover").IsHiddenAsync());
            var tagFilter = await page.Locator(".task-filter-popover-tags").BoundingBoxAsync();
            var typeFilter = await page.Locator(".task-filter-popover-field[for='task-type-filter']").BoundingBoxAsync();
            var statusFilter = await page.Locator(".task-filter-popover-field[for='task-status-filter']").BoundingBoxAsync();
            var priorityFilter = await page.Locator(".task-filter-popover-field[for='task-priority-filter']").BoundingBoxAsync();
            var typeSelect = await page.Locator("#task-type-filter").BoundingBoxAsync();
            var statusSelect = await page.Locator("#task-status-filter").BoundingBoxAsync();
            Assert.NotNull(tagFilter);
            Assert.NotNull(typeFilter);
            Assert.NotNull(statusFilter);
            Assert.NotNull(priorityFilter);
            Assert.NotNull(typeSelect);
            Assert.NotNull(statusSelect);
            Assert.True(tagFilter.Y <= typeFilter.Y);
            Assert.True(typeFilter.Y <= statusFilter.Y);
            Assert.True(statusFilter.Y <= priorityFilter.Y);
            Assert.InRange(Math.Abs(typeSelect.Height - statusSelect.Height), 0, 1);
            Assert.Equal(
                "task-filter-menu",
                await page.Locator("#task-filter-popover").EvaluateAsync<string>("popover => popover.parentElement.className"));
            Assert.Equal(
                "static",
                await page.Locator("#task-filter-popover").EvaluateAsync<string>("popover => getComputedStyle(popover).position"));

            await OpenAppearancePreferencesAsync(page);
            await page.Locator("[data-preference-select='task-filter-layout'] .preference-choice[data-value='COMPACT']").ClickAsync();
            await page.WaitForFunctionAsync(
                "() => !document.documentElement.classList.contains('task-filter-layout-expanded') && document.querySelector('#save-status').textContent === 'Task filter layout saved'");
        });
    }

    [Fact]
    public async Task StackedLayoutPreference_KeepsTheBodyEditorVisible()
    {
        await RunWorkspaceTestAsync("triage-stacked-preference", seedSampleTasks: true, test: async (page, fixture) =>
        {
            await WaitForFirstTaskLoadedAsync(page);
            await OpenAppearancePreferencesAsync(page);
            await page.Locator(".preferences-layout-control .preference-choice[data-value='STACKED']").ClickAsync();
            await page.WaitForFunctionAsync(
                "() => document.documentElement.classList.contains('layout-mode-stacked') && document.querySelector('#save-status').textContent === 'Layout preference saved'");
            await page.Locator("#settings-close-button").ClickAsync();

            Assert.True(await page.Locator(".task-view-rail").IsHiddenAsync());
            Assert.False(await page.Locator(".task-view-compact").IsHiddenAsync());
            var list = await page.Locator(".task-sidebar").BoundingBoxAsync();
            var resizer = await page.Locator("#layout-resizer").BoundingBoxAsync();
            var editor = await page.Locator(".task-editor-panel").BoundingBoxAsync();
            var body = await page.Locator(".editor-host").BoundingBoxAsync();
            Assert.NotNull(list);
            Assert.NotNull(resizer);
            Assert.NotNull(editor);
            Assert.NotNull(body);
            Assert.True(list.Y < resizer.Y);
            Assert.True(resizer.Y < editor.Y);
            Assert.True(
                body.Y < editor.Y + editor.Height,
                "Expected the body editor to remain visible without first scrolling the stacked detail panel.");
            Assert.InRange(list.Height, 220, 455);
            await AssertNoHorizontalPageOverflowAsync(page);
            await CaptureWorkspaceAsync(page, "triage-command-stacked.png");
        });
    }

    /// <summary>
    /// Waits until the app has selected a task on startup and finished loading it, including
    /// the body editor, so layout measurements see the complete task details.
    /// </summary>
    private static async Task WaitForFirstTaskLoadedAsync(IPage page)
    {
        await page.Locator(".tox-tinymce").WaitForAsync();
        await page.WaitForFunctionAsync(
            "() => { const form = document.querySelector('#task-form'); return form && !form.hidden && !form.hasAttribute('aria-busy') && document.querySelector('#task-title')?.value; }");
    }
}
