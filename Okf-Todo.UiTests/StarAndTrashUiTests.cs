using Microsoft.Playwright;

namespace Okf_Todo.UiTests;

// Starring, Trash, undo, bulk actions and permanent deletion. Each test sets up only the
// state it needs; tasks that must already be starred or in Trash are prepared through the
// bridge so the test exercises just one feature through the UI.
public sealed partial class NewTaskDialogUiTests
{
    [Fact]
    public async Task Star_MarksTaskAndStarredViewListsOnlyStarredTasks()
    {
        await RunWorkspaceTestAsync("star-view", async (page, fixture) =>
        {
            await DismissFirstRunPromptAsync(page);
            Assert.Equal(0, await page.Locator("#trash-permanent-actions").CountAsync());
            Assert.Equal(0, await page.Locator("#task-detail-star-button").CountAsync());
            Assert.Equal(0, await page.Locator("#task-detail-menu-button").CountAsync());
            Assert.True(await page.Locator("#task-detail-context-menu-button").IsHiddenAsync());
            Assert.True(await page.Locator("#task-view-overflow-button").IsHiddenAsync());

            const string starredTitle = "Star browser contract";
            await CreateTaskThroughDialogAsync(page, starredTitle);
            await CreateTaskThroughDialogAsync(page, "Unstarred browser contract");

            var starredShell = TaskRowShell(page, starredTitle);
            await starredShell.Locator(".task-row").ClickAsync();
            await page.WaitForFunctionAsync(
                "title => document.querySelector('#task-title')?.value === title",
                starredTitle);
            await starredShell.Locator(".task-row-star-button").ClickAsync();
            await WaitForStarStateAsync(page, starredTitle, isStarred: true);

            await OpenTaskViewAsync(page, "starred", "Starred");
            Assert.Single(await page.Locator("#task-list .task-row").AllAsync());
            Assert.Contains(starredTitle, await page.Locator("#task-list .task-row").First.TextContentAsync());
        });
    }

    [Fact]
    public async Task RowTrash_CanBeUndoneFromTheToast()
    {
        await RunWorkspaceTestAsync("row-trash-undo", async (page, fixture) =>
        {
            await DismissFirstRunPromptAsync(page);
            const string title = "Trash and undo browser contract";
            await CreateTaskThroughDialogAsync(page, title);

            await TaskRowShell(page, title).Locator(".task-row-more").ClickAsync();
            await page.Locator("#task-action-menu [data-task-action='trash']").ClickAsync();
            await page.Locator("#task-undo-toast").WaitForAsync();
            await page.Locator("#task-list .empty-list").WaitForAsync();
            Assert.Contains("task.trash", fixture.BridgeMessageTypes);

            Assert.False(await page.Locator("#task-undo-button").IsDisabledAsync());
            await page.Locator("#task-undo-button").DispatchEventAsync("click");
            await page.Locator(".task-row").Filter(new LocatorFilterOptions { HasText = title }).WaitForAsync();
            Assert.Contains("task.trash.restore", fixture.BridgeMessageTypes);
        });
    }

    [Fact]
    public async Task BulkTrash_MovesAllSelectedTasksAndOffersNoPermanentDelete()
    {
        await RunWorkspaceTestAsync("bulk-trash", async (page, fixture) =>
        {
            await DismissFirstRunPromptAsync(page);
            foreach (var title in new[] { "Bulk trash one", "Bulk trash two", "Bulk trash three" })
            {
                await CreateTaskThroughDialogAsync(page, title);
            }

            await OpenTaskViewAsync(page, "all", "All");
            Assert.Equal(3, await page.Locator("#task-list .task-row").CountAsync());
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
        });
    }

    [Fact]
    public async Task TrashView_ShowsTasksReadOnlyWithStarHistoryAndTrashActions()
    {
        await RunWorkspaceTestAsync("trash-view", async (page, fixture) =>
        {
            await DismissFirstRunPromptAsync(page);
            // Titles must not contain each other: row filters match text case-insensitively.
            const string starredTitle = "Starred trash contract";
            const string unstarredTitle = "Plain trash contract";
            await PrepareTrashedTasksAsync(page, fixture, starredTitle, unstarredTitle);

            await OpenTaskViewAsync(page, "trash", "Trash", dispatchClick: true);
            Assert.Equal(2, await page.Locator("#task-list .task-row").CountAsync());
            await page.WaitForFunctionAsync(
                "() => document.querySelector('#complete-button')?.textContent === 'Restore'");
            await AssertCurrentTaskReadOnlyAsync(page, "Task is in Trash", "Restore");
            Assert.False(await page.Locator("#task-view-overflow-button").IsHiddenAsync());
            Assert.False(await page.Locator("#task-view-overflow-button").IsDisabledAsync());
            Assert.True(await page.Locator("#task-detail-context-menu-button").IsHiddenAsync());

            Assert.Equal(
                "Starred before moved to Trash",
                await TaskRowShell(page, starredTitle).Locator(".task-row-star-static").GetAttributeAsync("title"));
            Assert.Equal(0, await TaskRowShell(page, unstarredTitle).Locator(".task-row-star").CountAsync());

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
            await TaskRowShell(page, unstarredTitle).Locator(".task-row-select").CheckAsync();
            Assert.Equal(
                "fixed",
                await page.Locator("#task-selection-bar").EvaluateAsync<string>("element => getComputedStyle(element).position"));
            Assert.True(await page.Locator("#task-bulk-trash").IsHiddenAsync());
            Assert.False(await page.Locator("#task-bulk-delete-permanently").IsHiddenAsync());
            Assert.Equal(
                "Delete all selected permanently",
                await page.Locator("#task-bulk-delete-permanently").TextContentAsync());
        });
    }

    [Fact]
    public async Task BulkDeletePermanently_RemovesOnlyTheSelectedTrashedTask()
    {
        await RunWorkspaceTestAsync("bulk-delete-permanently", async (page, fixture) =>
        {
            await DismissFirstRunPromptAsync(page);
            const string keptTitle = "Kept in Trash";
            const string deletedTitle = "Deleted from Trash";
            await PrepareTrashedTasksAsync(page, fixture, keptTitle, deletedTitle);

            await OpenTaskViewAsync(page, "trash", "Trash", dispatchClick: true);
            Assert.Equal(2, await page.Locator("#task-list .task-row").CountAsync());
            await page.Locator("#task-select-mode-button").ClickAsync();
            await TaskRowShell(page, deletedTitle).Locator(".task-row-select").CheckAsync();
            await page.Locator("#task-bulk-delete-permanently").ClickAsync();
            await page.Locator("#confirmation-overlay").WaitForAsync();
            Assert.Equal("Delete permanently", await page.Locator("#confirmation-confirm-button").TextContentAsync());
            await page.Locator("#confirmation-confirm-button").ClickAsync();

            await page.WaitForFunctionAsync(
                "() => document.querySelector('#save-status')?.textContent === 'Task deleted permanently' && document.querySelectorAll('#task-list .task-row').length === 1");
            Assert.Contains("task.trash.delete", fixture.BridgeMessageTypes);
            Assert.Equal(1, await TaskRowShell(page, keptTitle).CountAsync());
            Assert.Equal(0, await TaskRowShell(page, deletedTitle).CountAsync());
        });
    }

    [Fact]
    public async Task RestoreFromTrash_KeepsStarAndAllowsUnstarringAndStarring()
    {
        // The task detail context menu only appears in narrow windows.
        await RunWorkspaceTestAsync("restore-from-trash", width: 820, height: 900, test: async (page, fixture) =>
        {
            await DismissFirstRunPromptAsync(page);
            const string title = "Restore starred browser contract";
            await PrepareTrashedTasksAsync(page, fixture, title);

            await OpenTaskViewAsync(page, "trash", "Trash", dispatchClick: true);
            await page.Locator(".task-row").Filter(new LocatorFilterOptions { HasText = title }).ClickAsync();
            await page.Locator("#task-read-only-reopen-button").ClickAsync();
            await page.WaitForFunctionAsync(
                "() => document.querySelector('#task-list-title')?.textContent === 'Active' && !document.querySelector('#task-title')?.disabled");
            Assert.Contains("task.trash.restore", fixture.BridgeMessageTypes);
            Assert.True(await page.Locator("#task-view-overflow-button").IsHiddenAsync());
            Assert.Equal(
                "true",
                await TaskRowShell(page, title).Locator(".task-row-star-button").GetAttributeAsync("aria-pressed"));

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
                "() => document.querySelector('#save-status')?.textContent === 'Star removed'");
            await WaitForStarStateAsync(page, title, isStarred: false);

            await page.Locator("#task-detail-context-menu-button").ClickAsync();
            Assert.Equal(
                "Star task",
                await page.Locator("#task-action-menu [data-task-action='toggle-star'] span:last-child").TextContentAsync());
            await page.Locator("#task-action-menu [data-task-action='toggle-star']").ClickAsync();
            await page.WaitForFunctionAsync(
                "() => document.querySelector('#save-status')?.textContent === 'Task starred'");
            await WaitForStarStateAsync(page, title, isStarred: true);
        });
    }

    [Fact]
    public async Task EmptyTrash_AlsoDeletesTasksHiddenBySearch()
    {
        await RunWorkspaceTestAsync("empty-trash", async (page, fixture) =>
        {
            await DismissFirstRunPromptAsync(page);
            await PrepareTrashedTasksAsync(page, fixture, "Empty Trash browser contract");

            await OpenTaskViewAsync(page, "trash", "Trash", dispatchClick: true);
            Assert.Equal(1, await page.Locator("#task-list .task-row").CountAsync());
            await page.Locator("#task-search").FillAsync("hide every trashed task");
            await page.Locator("#task-list .empty-list").WaitForAsync();
            Assert.False(await page.Locator("#task-view-overflow-button").IsHiddenAsync());
            Assert.False(await page.Locator("#task-view-overflow-button").IsDisabledAsync());

            await page.Locator("#task-view-overflow-button").ClickAsync();
            await page.Locator("#trash-empty-button").ClickAsync();
            await page.Locator("#confirmation-overlay").WaitForAsync();
            Assert.Equal("Permanently delete 1 task?", await page.Locator("#confirmation-title").TextContentAsync());
            Assert.Equal("Empty Trash", await page.Locator("#confirmation-confirm-button").TextContentAsync());
            Assert.Contains(
                "including items hidden by current search or filters",
                await page.Locator("#confirmation-message").TextContentAsync());
            await page.Locator("#confirmation-confirm-button").ClickAsync();

            await page.WaitForFunctionAsync(
                "() => document.querySelector('#task-view-overflow-button')?.hidden && document.querySelector('#task-list .empty-list')");
            Assert.Contains("task.trash.delete", fixture.BridgeMessageTypes);
        });
    }

    /// <summary>
    /// Creates tasks through the dialog, then stars the first one and moves them all to Trash
    /// through the bridge. The caller opens the Trash view to see them.
    /// </summary>
    private static async Task PrepareTrashedTasksAsync(IPage page, UiAppFixture fixture, string starredTitle, params string[] otherTitles)
    {
        var titles = new[] { starredTitle }.Concat(otherTitles).ToArray();
        foreach (var title in titles)
        {
            await CreateTaskThroughDialogAsync(page, title);
        }

        var taskIds = new List<int>();
        foreach (var title in titles)
        {
            var id = await TaskRowShell(page, title).GetAttributeAsync("data-task-id");
            taskIds.Add(int.Parse(id!));
        }

        await fixture.SendBridgeAsync("task.star.set", new { id = taskIds[0], isStarred = true });
        await fixture.SendBridgeAsync("task.trash", new { taskIds });
    }

    private static Task WaitForStarStateAsync(IPage page, string title, bool isStarred) =>
        page.WaitForFunctionAsync(
            "args => [...document.querySelectorAll('.task-row-shell')].find(shell => shell.textContent.includes(args.title))?.querySelector('.task-row-star-button')?.getAttribute('aria-pressed') === args.pressed",
            new { title, pressed = isStarred ? "true" : "false" });
}
