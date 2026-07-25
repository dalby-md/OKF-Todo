using Microsoft.EntityFrameworkCore;
using Photino.Okf_Todo.Services;

namespace Okf_Todo.Tests;

public sealed class TaskListServiceTests
{
    [Fact]
    public async Task EnsureDefaultListAsync_RecoversWhenNoListsExist()
    {
        await using var database = await TestDatabase.CreateAsync();
        database.DbContext.TaskLists.RemoveRange(await database.DbContext.TaskLists.ToListAsync());
        await database.DbContext.SaveChangesAsync();

        var recovered = await database.TaskLists.EnsureDefaultListAsync();

        Assert.Equal(TaskListService.DefaultListName, recovered.Name);
        Assert.Single(await database.TaskLists.ListAsync());
    }

    [Fact]
    public async Task ResolveAsync_UsesExplicitContextDefaultOrderAndZeroListPrecedence()
    {
        await using var database = await TestDatabase.CreateAsync();
        var defaultList = await database.TaskLists.EnsureDefaultListAsync();
        var second = await database.TaskLists.CreateAsync(new TaskListCreateRequest("Customer work"));
        var task = await database.Tasks.CreateAsync(CreateRequest("Context task", second.Id), CancellationToken.None);

        Assert.Equal(defaultList.Id, (await database.TaskLists.ResolveAsync(null, [], CancellationToken.None)).Id);
        Assert.Equal(second.Id, (await database.TaskLists.ResolveAsync(second.Id, [], CancellationToken.None)).Id);
        Assert.Equal(second.Id, (await database.TaskLists.ResolveAsync(null, [task.Id], CancellationToken.None)).Id);

        await database.TaskLists.RenameAsync(
            new TaskListRenameRequest(defaultList.Id, "Renamed default"),
            CancellationToken.None);
        await database.TaskLists.ReorderAsync(
            new TaskListReorderRequest([second.Id, defaultList.Id]),
            CancellationToken.None);
        Assert.Equal(second.Id, (await database.TaskLists.ResolveAsync(null, [], CancellationToken.None)).Id);
    }

    [Fact]
    public async Task DeleteAsync_MovesEveryTaskIncludingTrashAndWritesTimeline()
    {
        await using var database = await TestDatabase.CreateAsync();
        var source = await database.TaskLists.CreateAsync(new TaskListCreateRequest("Temporary"));
        var destination = await database.TaskLists.EnsureDefaultListAsync();
        var first = await database.Tasks.CreateAsync(CreateRequest("Active task", source.Id), CancellationToken.None);
        var second = await database.Tasks.CreateAsync(CreateRequest("Trash task", source.Id), CancellationToken.None);
        await database.Tasks.MoveToTrashAsync(new TaskIdsRequest([second.Id]), CancellationToken.None);

        var result = await database.TaskLists.DeleteAsync(
            new TaskListDeleteRequest(source.Id, destination.Id),
            CancellationToken.None);

        Assert.Equal(2, result.MovedTaskCount);
        Assert.False(await database.DbContext.TaskLists.AnyAsync(item => item.Id == source.Id));
        Assert.All(
            await database.DbContext.TaskItems.Where(task => task.Id == first.Id || task.Id == second.Id).ToListAsync(),
            task => Assert.Equal(destination.Id, task.TaskListId));
        Assert.All(
            new[] { first.Id, second.Id },
            taskId => Assert.Contains(
                database.DbContext.TaskLogEntries.Where(log => log.TaskId == taskId),
                log => log.Message.Contains("List deleted: Moved from", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task MoveAndUndo_RestoreOriginalListsAndWriteTimeline()
    {
        await using var database = await TestDatabase.CreateAsync();
        var defaultList = await database.TaskLists.EnsureDefaultListAsync();
        var destination = await database.TaskLists.CreateAsync(new TaskListCreateRequest("Release"));
        var task = await database.Tasks.CreateAsync(CreateRequest("Prepare release", defaultList.Id), CancellationToken.None);

        var moved = await database.TaskLists.MoveTasksAsync(
            new TaskListMoveRequest([task.Id], destination.Id),
            CancellationToken.None);
        await database.TaskLists.UndoMoveAsync(
            new TaskListUndoMoveRequest(moved.Items),
            CancellationToken.None);

        Assert.Equal(defaultList.Id, (await database.DbContext.TaskItems.SingleAsync(item => item.Id == task.Id)).TaskListId);
        var messages = await database.DbContext.TaskLogEntries
            .Where(log => log.TaskId == task.Id)
            .Select(log => log.Message)
            .ToListAsync();
        Assert.Contains(messages, message => message.Contains("Task moved: Moved from", StringComparison.Ordinal));
        Assert.Contains(messages, message => message.Contains("List move undone: Moved from", StringComparison.Ordinal));
    }

    [Fact]
    public async Task NamesAreCaseInsensitiveAndFinalListCannotBeDeleted()
    {
        await using var database = await TestDatabase.CreateAsync();
        var defaultList = await database.TaskLists.EnsureDefaultListAsync();

        await Assert.ThrowsAsync<ValidationException>(() =>
            database.TaskLists.CreateAsync(new TaskListCreateRequest(" default LIST ")));
        await Assert.ThrowsAsync<ValidationException>(() =>
            database.TaskLists.DeleteAsync(new TaskListDeleteRequest(defaultList.Id, null)));
    }

    [Fact]
    public async Task ScopedAndGlobalQueriesReturnExpectedOwnership()
    {
        await using var database = await TestDatabase.CreateAsync();
        var first = await database.TaskLists.EnsureDefaultListAsync();
        var second = await database.TaskLists.CreateAsync(new TaskListCreateRequest("Support"));
        await database.Tasks.CreateAsync(CreateRequest("First", first.Id), CancellationToken.None);
        await database.Tasks.CreateAsync(CreateRequest("Second", second.Id), CancellationToken.None);

        var scoped = await database.Tasks.ListAsync(new TaskListRequest("all", second.Id), CancellationToken.None);
        var global = await database.Tasks.ListAsync(new TaskListRequest("all", null), CancellationToken.None);

        Assert.Single(scoped);
        Assert.Equal("Support", scoped.Single().TaskListName);
        Assert.Equal(2, global.Count);
    }

    private static TaskSaveRequest CreateRequest(string title, int taskListId) =>
        new(
            Id: null,
            Title: title,
            TaskTypeCode: "ERROR",
            Body: null,
            BodyFormatCode: "HTML",
            TaskPriorityCode: "NORMAL",
            TaskSourceCode: null,
            SourceReference: null,
            SourceUrl: null,
            Deadline: null,
            TaskListId: taskListId);
}
