using System.ComponentModel;
using ModelContextProtocol.Server;
using Photino.Okf_Todo.Services;

namespace Photino.Okf_Todo.Mcp;

[McpServerToolType]
public static class TaskLifecycleTools
{
    [McpServerTool(Name = "task_complete", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Complete an active task and record the lifecycle transition in its Timeline.")]
    public static Task<TaskDetailDto> CompleteAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int id,
        CancellationToken cancellationToken = default) =>
        ExecuteTaskAsync(commandService, "task.complete", id, cancellationToken);

    [McpServerTool(Name = "task_cancel", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Cancel an active task and record the lifecycle transition in its Timeline.")]
    public static Task<TaskDetailDto> CancelAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int id,
        CancellationToken cancellationToken = default) =>
        ExecuteTaskAsync(commandService, "task.cancel", id, cancellationToken);

    [McpServerTool(Name = "task_reopen", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Reopen a completed or cancelled task as active.")]
    public static Task<TaskDetailDto> ReopenAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int id,
        CancellationToken cancellationToken = default) =>
        ExecuteTaskAsync(commandService, "task.reopen", id, cancellationToken);

    [McpServerTool(Name = "task_set_starred", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Star or unstar one task without changing its lifecycle state.")]
    public static Task<TaskDetailDto> SetStarredAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int id,
        [Description("True to star; false to unstar.")] bool isStarred,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<TaskDetailDto>(
            commandService, "task.star.set", new TaskStarRequest(id, isStarred), cancellationToken);

    [McpServerTool(Name = "task_bulk_set_starred", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Star or unstar several non-Trash tasks in one operation.")]
    public static Task<TaskBulkActionResult> BulkSetStarredAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task IDs.")] IReadOnlyCollection<int> taskIds,
        [Description("True to star; false to unstar.")] bool isStarred,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<TaskBulkActionResult>(
            commandService,
            "task.star.setMany",
            new TaskBulkStarRequest(taskIds, isStarred),
            cancellationToken);

    [McpServerTool(Name = "task_set_waiting", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Set or replace the active waiting target while keeping the task active.")]
    public static Task<TaskDetailDto> SetWaitingAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int id,
        [Description("Non-empty waiting-for label.")] string label,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<TaskDetailDto>(
            commandService, "task.waiting.add", new TaskWaitingForSaveRequest(id, label), cancellationToken);

    [McpServerTool(Name = "task_clear_waiting", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Resolve and clear the task's active waiting target.")]
    public static Task<TaskDetailDto> ClearWaitingAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int id,
        CancellationToken cancellationToken = default) =>
        ExecuteTaskAsync(commandService, "task.waiting.clear", id, cancellationToken);

    [McpServerTool(Name = "task_move_to_trash", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Move one or more tasks to reversible Trash. Task content and lifecycle state are preserved.")]
    public static Task<TaskBulkActionResult> MoveToTrashAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task IDs.")] IReadOnlyCollection<int> taskIds,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<TaskBulkActionResult>(
            commandService, "task.trash", new TaskIdsRequest(taskIds), cancellationToken);

    [McpServerTool(Name = "task_restore_from_trash", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Restore one or more tasks from Trash to normal views.")]
    public static Task<TaskBulkActionResult> RestoreFromTrashAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task IDs currently in Trash.")] IReadOnlyCollection<int> taskIds,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<TaskBulkActionResult>(
            commandService, "task.trash.restore", new TaskIdsRequest(taskIds), cancellationToken);

    private static Task<TaskDetailDto> ExecuteTaskAsync(
        ApplicationCommandService commandService,
        string commandType,
        int id,
        CancellationToken cancellationToken) =>
        McpToolExecutor.ExecuteAsync<TaskDetailDto>(
            commandService, commandType, new TaskIdRequest(id), cancellationToken);
}
