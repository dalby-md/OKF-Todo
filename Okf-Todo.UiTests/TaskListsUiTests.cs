using Microsoft.Playwright;

namespace Okf_Todo.UiTests;

// Task lists: the list manager, creating tasks in a list, moving tasks between lists, and
// the list switcher in the workspace header. Lists a test needs but does not exercise are
// created through the bridge before the page opens.
public sealed partial class NewTaskDialogUiTests
{
    private const string SupportListName = "Support";

    [Fact]
    public async Task TaskListManager_CreatesRenamesDeletesAndReordersLists()
    {
        await RunWorkspaceTestAsync("task-list-manager", width: 1600, height: 1000, failOnConsoleErrors: true, test: async (page, fixture) =>
        {
            await DismissFirstRunPromptAsync(page);
            await OpenTaskListManagerAsync(page);
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
            await page.Locator("#task-list-detail-name").FillAsync(SupportListName);
            Assert.Equal("Save changes", await page.Locator("#task-list-detail-save").TextContentAsync());
            await page.Locator("#task-list-detail-save").ClickAsync();
            await page.WaitForFunctionAsync(
                "name => [...document.querySelectorAll('#task-list-switcher option')].some(option => option.textContent === name)",
                SupportListName);

            await page.Locator("#task-list-new-button").ClickAsync();
            await page.Locator("#task-list-detail-name").FillAsync("Temporary");
            await page.Locator("#task-list-detail-save").ClickAsync();
            await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-list-switcher option').length === 4");
            await TaskListManagerRow(page, "Default list").Locator(".task-list-manager-select").ClickAsync();
            await CaptureViewportAsync(page, "task-list-manager-master-detail.png");
            await TaskListManagerRow(page, "Temporary").Locator(".task-list-manager-select").ClickAsync();
            await page.Locator("#task-list-delete-button").ClickAsync();
            await page.Locator("#task-list-delete-overlay").WaitForAsync();
            Assert.Equal("Delete list", await page.Locator("#task-list-delete-confirm").TextContentAsync());
            await page.Locator("#task-list-delete-confirm").ClickAsync();
            await page.WaitForFunctionAsync("() => document.querySelectorAll('#task-list-switcher option').length === 3");

            await TaskListManagerRow(page, SupportListName).DragToAsync(TaskListManagerRow(page, "Default list"));
            await page.WaitForFunctionAsync("() => document.querySelector('#save-status')?.textContent === 'List order saved'");
            Assert.Equal(
                SupportListName,
                await page.Locator(".task-list-manager-row").First.Locator(".task-list-manager-name").TextContentAsync());

            Assert.Contains("taskList.create", fixture.BridgeMessageTypes);
            Assert.Contains("taskList.rename", fixture.BridgeMessageTypes);
            Assert.Contains("taskList.reorder", fixture.BridgeMessageTypes);
            Assert.Contains("taskList.delete", fixture.BridgeMessageTypes);
        });
    }

    [Fact]
    public async Task TaskListManager_StacksListAndDetailInNarrowWindows()
    {
        await RunWorkspaceTestAsync("task-list-manager-narrow", width: 1600, height: 1000, failOnConsoleErrors: true, prepare: CreateSupportListAsync, test: async (page, fixture) =>
        {
            await DismissFirstRunPromptAsync(page);
            await OpenTaskListManagerAsync(page);

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
        });
    }

    [Fact]
    public async Task NewTask_CanBeCreatedInAChosenListAndAllListsScopeIsRemembered()
    {
        await RunWorkspaceTestAsync("task-list-new-task", width: 1600, height: 1000, failOnConsoleErrors: true, prepare: CreateSupportListAsync, test: async (page, fixture) =>
        {
            await DismissFirstRunPromptAsync(page);
            await SelectAllListsScopeAsync(page);

            await page.Locator("#new-task-button").ClickAsync();
            await page.Locator("#new-task-title-input").FillAsync("List-aware support task");
            Assert.False(await page.Locator("#new-task-list-field").IsHiddenAsync());
            await page.Locator("#new-task-list-select").SelectOptionAsync(new SelectOptionValue { Label = SupportListName });
            await page.Locator("#new-task-save-button").ClickAsync();
            await page.Locator("#new-task-overlay").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
            await page.WaitForFunctionAsync("() => document.querySelector('#save-status')?.textContent === 'Saved'");

            Assert.Equal(SupportListName, await page.Locator("#task-list-owner option:checked").TextContentAsync());
            Assert.Equal(SupportListName, await page.Locator(".task-row.is-selected .task-list-pill").TextContentAsync());

            await page.Locator("#task-search").FillAsync(SupportListName);
            Assert.Equal(1, await page.Locator(".task-row-shell").CountAsync());
            await page.Locator("#task-search").FillAsync(string.Empty);

            await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.WaitForFunctionAsync("() => document.querySelector('#task-list-switcher')?.value === 'ALL'");
            Assert.Contains("layout.preference.save", fixture.BridgeMessageTypes);
        });
    }

    [Fact]
    public async Task MoveToList_FromTheRowMenuCanBeUndoneInAllListsAndSingleListScope()
    {
        await RunWorkspaceTestAsync("task-list-move", width: 1600, height: 1000, failOnConsoleErrors: true, prepare: CreateSupportListAsync, test: async (page, fixture) =>
        {
            await DismissFirstRunPromptAsync(page);
            const string title = "List-aware support task";
            await SelectAllListsScopeAsync(page);
            await CreateTaskInListThroughDialogAsync(page, title, SupportListName);

            await TaskRowShell(page, title).Locator(".task-row-more").ClickAsync();
            var moveToListAction = page.Locator("#task-action-menu [data-task-action='move-list']");
            Assert.False(await moveToListAction.IsHiddenAsync());
            Assert.Equal("Move to list", await moveToListAction.Locator("span:last-child").TextContentAsync());
            await CaptureWorkspaceAsync(page, "task-menu-move-to-list.png");
            await moveToListAction.ClickAsync();
            Assert.Equal("Move task to list", await page.Locator("#task-list-move-title").TextContentAsync());
            Assert.Equal(1, await page.Locator("#task-list-move-destination option").CountAsync());
            Assert.Equal("Default list", await page.Locator("#task-list-move-destination option").TextContentAsync());
            await page.Locator("#task-list-move-confirm").ClickAsync();
            await WaitForSelectedTaskListPillAsync(page, "Default list");
            await page.Locator("#task-undo-button").ClickAsync();
            await WaitForSelectedTaskListPillAsync(page, SupportListName);

            // In a single-list scope, moving a task follows it to the destination list.
            await page.Locator("#task-list-switcher").SelectOptionAsync(new SelectOptionValue { Label = SupportListName });
            await WaitForListScopeAndTaskAsync(page, SupportListName, title);
            await page.Locator(".task-row-shell .task-row-more").ClickAsync();
            await page.Locator("#task-action-menu [data-task-action='move-list']").ClickAsync();
            await page.Locator("#task-list-move-confirm").ClickAsync();
            await WaitForListScopeAndTaskAsync(page, "Default list", title);
            await page.Locator("#task-undo-button").ClickAsync();
            await WaitForListScopeAndTaskAsync(page, SupportListName, title);

            Assert.Contains("taskList.moveTasks", fixture.BridgeMessageTypes);
            Assert.Contains("taskList.undoMove", fixture.BridgeMessageTypes);
        });
    }

    [Fact]
    public async Task BulkMoveToList_CanBeUndone()
    {
        await RunWorkspaceTestAsync("task-list-bulk-move", width: 1600, height: 1000, failOnConsoleErrors: true, prepare: CreateSupportListAsync, test: async (page, fixture) =>
        {
            await DismissFirstRunPromptAsync(page);
            await SelectAllListsScopeAsync(page);
            await CreateTaskInListThroughDialogAsync(page, "Bulk move support task", SupportListName);

            await page.Locator("#task-select-mode-button").ClickAsync();
            await page.Locator(".task-row-select").CheckAsync();
            await page.Locator("#task-bulk-move-list").ClickAsync();
            await page.Locator("#task-list-move-destination").SelectOptionAsync(new SelectOptionValue { Label = "Default list" });
            await page.Locator("#task-list-move-confirm").ClickAsync();
            await WaitForSelectedTaskListPillAsync(page, "Default list");
            await page.Locator("#task-undo-button").ClickAsync();
            await WaitForSelectedTaskListPillAsync(page, SupportListName);

            Assert.Contains("taskList.moveTasks", fixture.BridgeMessageTypes);
            Assert.Contains("taskList.undoMove", fixture.BridgeMessageTypes);
        });
    }

    [Fact]
    public async Task WorkspaceHeader_ListSwitcherAdaptsAcrossWindowSizes()
    {
        await RunWorkspaceTestAsync("workspace-header", width: 1600, height: 1000, failOnConsoleErrors: true, prepare: CreateSupportListAsync, test: async (page, fixture) =>
        {
            await DismissFirstRunPromptAsync(page);
            await SelectAllListsScopeAsync(page);

            var largeBrandBox = await page.Locator(".app-brand").BoundingBoxAsync();
            var largeSwitcherBox = await page.Locator(".task-list-switcher-group").BoundingBoxAsync();
            var largeRailBox = await page.Locator(".task-view-rail").BoundingBoxAsync();
            Assert.NotNull(largeBrandBox);
            Assert.NotNull(largeSwitcherBox);
            Assert.NotNull(largeRailBox);
            Assert.InRange(Math.Abs(largeSwitcherBox!.Y - largeBrandBox!.Y), 0, 8);
            Assert.InRange(Math.Abs(largeSwitcherBox.X - (largeRailBox!.X + largeRailBox.Width)), 0, 1);
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
            Assert.InRange(Math.Abs(densitySwitcherBox.X - (densityRailBox!.X + densityRailBox.Width)), 0, 1);
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
        });
    }

    private static Task CreateSupportListAsync(UiAppFixture fixture) =>
        fixture.SendBridgeAsync("taskList.create", new { name = SupportListName });

    private static async Task OpenTaskListManagerAsync(IPage page)
    {
        await page.Locator("#manage-task-lists-button").ClickAsync();
        await page.Locator("#task-lists-overlay").WaitForAsync();
    }

    private static async Task SelectAllListsScopeAsync(IPage page)
    {
        await page.Locator("#task-list-switcher").SelectOptionAsync("ALL");
        await page.WaitForFunctionAsync(
            "() => document.querySelector('#task-list-switcher')?.value === 'ALL' && !document.querySelector('#task-list').hasAttribute('aria-busy')");
    }

    private static async Task CreateTaskInListThroughDialogAsync(IPage page, string title, string listName)
    {
        await page.Locator("#new-task-button").ClickAsync();
        await page.Locator("#new-task-title-input").FillAsync(title);
        await page.Locator("#new-task-list-select").SelectOptionAsync(new SelectOptionValue { Label = listName });
        await page.Locator("#new-task-save-button").ClickAsync();
        await page.Locator("#new-task-overlay").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
        await page.WaitForFunctionAsync("() => document.querySelector('#save-status')?.textContent === 'Saved'");
    }

    private static ILocator TaskListManagerRow(IPage page, string listName) =>
        page.Locator(".task-list-manager-row").Filter(new LocatorFilterOptions { HasText = listName });

    private static Task WaitForSelectedTaskListPillAsync(IPage page, string listName) =>
        page.WaitForFunctionAsync(
            "name => document.querySelector('.task-row.is-selected .task-list-pill')?.textContent.trim() === name",
            listName);

    private static Task WaitForListScopeAndTaskAsync(IPage page, string listName, string taskTitle) =>
        page.WaitForFunctionAsync(
            "args => document.querySelector('#task-list-switcher option:checked')?.textContent === args.listName && document.querySelector('#task-title')?.value === args.taskTitle",
            new { listName, taskTitle });
}
