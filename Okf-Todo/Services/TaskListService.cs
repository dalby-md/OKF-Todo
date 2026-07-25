using Microsoft.EntityFrameworkCore;
using Photino.Okf_Todo.Data;

namespace Photino.Okf_Todo.Services;

public sealed class TaskListService(AppDbContext dbContext)
{
    public const string DefaultListName = "Default list";

    public async Task<TaskList> EnsureDefaultListAsync(CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.TaskLists
            .OrderBy(taskList => taskList.SortOrder)
            .ThenBy(taskList => taskList.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        existing = await dbContext.TaskLists
            .OrderBy(taskList => taskList.SortOrder)
            .ThenBy(taskList => taskList.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is null)
        {
            var now = DateTime.UtcNow;
            existing = new TaskList
            {
                Name = DefaultListName,
                SortOrder = 10,
                CreatedAt = now,
                UpdatedAt = now
            };
            dbContext.TaskLists.Add(existing);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return existing;
    }

    public async Task<TaskList> ResolveAsync(
        int? explicitListId,
        IEnumerable<int?>? contextTaskIds,
        CancellationToken cancellationToken = default)
    {
        if (explicitListId is > 0)
        {
            return await GetRequiredAsync(explicitListId.Value, cancellationToken);
        }

        foreach (var contextTaskId in contextTaskIds ?? [])
        {
            if (contextTaskId is not > 0)
            {
                continue;
            }

            var contextListId = await dbContext.TaskItems
                .AsNoTracking()
                .Where(task => task.Id == contextTaskId.Value)
                .Select(task => (int?)task.TaskListId)
                .SingleOrDefaultAsync(cancellationToken);
            if (contextListId is > 0)
            {
                return await GetRequiredAsync(contextListId.Value, cancellationToken);
            }
        }

        var defaultList = await dbContext.TaskLists
            .FirstOrDefaultAsync(
                taskList => taskList.Name == DefaultListName,
                cancellationToken);
        if (defaultList is not null)
        {
            return defaultList;
        }

        var firstList = await dbContext.TaskLists
            .OrderBy(taskList => taskList.SortOrder)
            .ThenBy(taskList => taskList.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return firstList ?? await EnsureDefaultListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<TaskListDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultListAsync(cancellationToken);
        return await dbContext.TaskLists
            .AsNoTracking()
            .OrderBy(taskList => taskList.SortOrder)
            .ThenBy(taskList => taskList.Id)
            .Select(taskList => new TaskListDto(
                taskList.Id,
                taskList.Name,
                taskList.SortOrder,
                taskList.Tasks.Count,
                taskList.Tasks.Count(task => task.DeletedAt != null),
                taskList.CreatedAt,
                taskList.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskListDto> CreateAsync(
        TaskListCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = NormalizeName(request.Name);
        await EnsureUniqueNameAsync(name, null, cancellationToken);
        var nextSortOrder = (await dbContext.TaskLists
            .MaxAsync(taskList => (int?)taskList.SortOrder, cancellationToken) ?? 0) + 10;
        var now = DateTime.UtcNow;
        var taskList = new TaskList
        {
            Name = name,
            SortOrder = nextSortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.TaskLists.Add(taskList);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(taskList, 0, 0);
    }

    public async Task<TaskListDto> RenameAsync(
        TaskListRenameRequest request,
        CancellationToken cancellationToken = default)
    {
        var taskList = await GetRequiredAsync(request.Id, cancellationToken);
        var name = NormalizeName(request.Name);
        await EnsureUniqueNameAsync(name, taskList.Id, cancellationToken);
        taskList.Name = name;
        taskList.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        var counts = await GetCountsAsync(taskList.Id, cancellationToken);
        return ToDto(taskList, counts.Total, counts.Trash);
    }

    public async Task<IReadOnlyCollection<TaskListDto>> ReorderAsync(
        TaskListReorderRequest request,
        CancellationToken cancellationToken = default)
    {
        var orderedIds = request.OrderedIds.Distinct().ToArray();
        var taskLists = await dbContext.TaskLists.ToListAsync(cancellationToken);
        if (orderedIds.Length != taskLists.Count
            || taskLists.Any(taskList => !orderedIds.Contains(taskList.Id)))
        {
            throw new ValidationException("The ordered list must contain every task list exactly once.", "orderedIds");
        }

        var now = DateTime.UtcNow;
        for (var index = 0; index < orderedIds.Length; index++)
        {
            var taskList = taskLists.Single(item => item.Id == orderedIds[index]);
            taskList.SortOrder = (index + 1) * 10;
            taskList.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await ListAsync(cancellationToken);
    }

    public async Task<TaskListDeleteResult> DeleteAsync(
        TaskListDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var taskList = await GetRequiredAsync(request.Id, cancellationToken);
        if (await dbContext.TaskLists.CountAsync(cancellationToken) <= 1)
        {
            throw new ValidationException("The final remaining task list cannot be deleted.", "listId");
        }

        var tasks = await dbContext.TaskItems
            .Include(task => task.TaskList)
            .Where(task => task.TaskListId == taskList.Id)
            .ToListAsync(cancellationToken);

        TaskList? destination = null;
        if (tasks.Count > 0)
        {
            if (request.DestinationListId is null)
            {
                throw new ValidationException(
                    "Choose a destination list. Deleting a list never deletes its tasks.",
                    "destinationListId");
            }

            if (request.DestinationListId == taskList.Id)
            {
                throw new ValidationException("Choose a different destination list.", "destinationListId");
            }

            destination = await GetRequiredAsync(request.DestinationListId.Value, cancellationToken);
            await MoveTrackedTasksAsync(tasks, destination, "List deleted", cancellationToken);
        }

        dbContext.TaskLists.Remove(taskList);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new TaskListDeleteResult(
            taskList.Id,
            taskList.Name,
            destination?.Id,
            destination?.Name,
            tasks.Count);
    }

    public async Task<TaskListMoveResult> MoveTasksAsync(
        TaskListMoveRequest request,
        CancellationToken cancellationToken = default)
    {
        var taskIds = NormalizeTaskIds(request.TaskIds);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var destination = await GetRequiredAsync(request.DestinationListId, cancellationToken);
        var tasks = await dbContext.TaskItems
            .Include(task => task.TaskList)
            .Where(task => taskIds.Contains(task.Id) && task.DeletedAt == null)
            .ToListAsync(cancellationToken);
        if (tasks.Count != taskIds.Count)
        {
            throw new ValidationException(
                "One or more tasks were not found or must be restored before moving.",
                "taskIds");
        }

        var originalItems = tasks
            .Where(task => task.TaskListId != destination.Id)
            .Select(task => new TaskListMoveItemDto(
                task.Id,
                task.TaskListId,
                task.TaskList?.Name ?? string.Empty,
                destination.Id,
                destination.Name))
            .ToArray();
        await MoveTrackedTasksAsync(tasks, destination, "Task moved", cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new TaskListMoveResult(originalItems);
    }

    public async Task<TaskListMoveResult> UndoMoveAsync(
        TaskListUndoMoveRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            throw new ValidationException("There is no list move to undo.", "items");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var taskIds = request.Items.Select(item => item.TaskId).Distinct().ToArray();
        var tasks = await dbContext.TaskItems
            .Include(task => task.TaskList)
            .Where(task => taskIds.Contains(task.Id) && task.DeletedAt == null)
            .ToListAsync(cancellationToken);
        if (tasks.Count != taskIds.Length)
        {
            throw new ValidationException(
                "One or more tasks were not found or must be restored before undoing the move.",
                "items");
        }

        var result = new List<TaskListMoveItemDto>();
        foreach (var group in request.Items.GroupBy(item => item.OriginalListId))
        {
            var destination = await GetRequiredAsync(group.Key, cancellationToken);
            var groupIds = group.Select(item => item.TaskId).ToHashSet();
            var groupTasks = tasks.Where(task => groupIds.Contains(task.Id)).ToList();
            result.AddRange(groupTasks
                .Where(task => task.TaskListId != destination.Id)
                .Select(task => new TaskListMoveItemDto(
                    task.Id,
                    task.TaskListId,
                    task.TaskList?.Name ?? string.Empty,
                    destination.Id,
                    destination.Name)));
            await MoveTrackedTasksAsync(groupTasks, destination, "List move undone", cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return new TaskListMoveResult(result);
    }

    public async Task LogTaskMoveAsync(
        TaskItem task,
        TaskList source,
        TaskList destination,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (source.Id == destination.Id)
        {
            return;
        }

        var now = DateTime.UtcNow;
        task.TaskListId = destination.Id;
        task.TaskList = destination;
        task.UpdatedAt = now;
        task.LogEntries.Add(new TaskLogEntry
        {
            TaskLogType = await GetOrCreateTaskUpdatedLogTypeAsync(cancellationToken),
            Message = $"{reason}: Moved from '{source.Name}' to '{destination.Name}'",
            OldValue = source.Name,
            NewValue = destination.Name,
            CreatedAt = now
        });
    }

    private async Task MoveTrackedTasksAsync(
        IReadOnlyCollection<TaskItem> tasks,
        TaskList destination,
        string reason,
        CancellationToken cancellationToken)
    {
        foreach (var task in tasks)
        {
            if (task.TaskListId == destination.Id)
            {
                continue;
            }

            var source = task.TaskList ?? await GetRequiredAsync(task.TaskListId, cancellationToken);
            await LogTaskMoveAsync(task, source, destination, reason, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<TaskList> GetRequiredAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.TaskLists.SingleOrDefaultAsync(taskList => taskList.Id == id, cancellationToken)
            ?? throw new ValidationException("Task list was not found.", "listId");
    }

    private async Task EnsureUniqueNameAsync(
        string name,
        int? exceptId,
        CancellationToken cancellationToken)
    {
        if (await dbContext.TaskLists.AnyAsync(
                taskList => taskList.Name == name && taskList.Id != exceptId,
                cancellationToken))
        {
            throw new ValidationException("Task list names must be unique.", "name");
        }
    }

    private async Task<TaskLogType> GetOrCreateTaskUpdatedLogTypeAsync(CancellationToken cancellationToken)
    {
        var logType = await dbContext.TaskLogTypes
            .SingleOrDefaultAsync(item => item.Code == TaskLogTypeCodes.TaskUpdated, cancellationToken);
        if (logType is not null)
        {
            return logType;
        }

        var now = DateTime.UtcNow;
        logType = new TaskLogType
        {
            Code = TaskLogTypeCodes.TaskUpdated,
            Name = "Task updated",
            SortOrder = 230,
            IsActive = true,
            IsSystem = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.TaskLogTypes.Add(logType);
        return logType;
    }

    private async Task<(int Total, int Trash)> GetCountsAsync(
        int taskListId,
        CancellationToken cancellationToken)
    {
        var total = await dbContext.TaskItems.CountAsync(task => task.TaskListId == taskListId, cancellationToken);
        var trash = await dbContext.TaskItems.CountAsync(
            task => task.TaskListId == taskListId && task.DeletedAt != null,
            cancellationToken);
        return (total, trash);
    }

    private static TaskListDto ToDto(TaskList taskList, int taskCount, int trashTaskCount) =>
        new(
            taskList.Id,
            taskList.Name,
            taskList.SortOrder,
            taskCount,
            trashTaskCount,
            taskList.CreatedAt,
            taskList.UpdatedAt);

    private static string NormalizeName(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ValidationException("Task list name is required.", "name")
            : value.Trim();
    }

    private static IReadOnlyCollection<int> NormalizeTaskIds(IReadOnlyCollection<int>? taskIds)
    {
        var normalized = (taskIds ?? [])
            .Where(taskId => taskId > 0)
            .Distinct()
            .ToArray();
        if (normalized.Length == 0)
        {
            throw new ValidationException("Select at least one task.", "taskIds");
        }

        return normalized;
    }
}

public sealed record TaskListDto(
    int Id,
    string Name,
    int SortOrder,
    int TaskCount,
    int TrashTaskCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record TaskListCreateRequest(string Name);

public sealed record TaskListRenameRequest(int Id, string Name);

public sealed record TaskListReorderRequest(IReadOnlyCollection<int> OrderedIds);

public sealed record TaskListDeleteRequest(int Id, int? DestinationListId);

public sealed record TaskListDeleteResult(
    int DeletedListId,
    string DeletedListName,
    int? DestinationListId,
    string? DestinationListName,
    int MovedTaskCount);

public sealed record TaskListMoveRequest(
    IReadOnlyCollection<int> TaskIds,
    int DestinationListId);

public sealed record TaskListMoveItemDto(
    int TaskId,
    int OriginalListId,
    string OriginalListName,
    int DestinationListId,
    string DestinationListName);

public sealed record TaskListMoveResult(IReadOnlyCollection<TaskListMoveItemDto> Items)
{
    public int AffectedCount => Items.Count;
}

public sealed record TaskListUndoMoveRequest(IReadOnlyCollection<TaskListMoveItemDto> Items);
